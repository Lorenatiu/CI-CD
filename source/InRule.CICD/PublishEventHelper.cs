using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using InRule.CICD.Helpers;

namespace InRule.CICD
{
    public class PublishEventHelper
    {
        public static void WriteToSlack(string eventType, object data, string messagePreffix)
        {
            try
            {
                var map = data as IDictionary<string, object>;

                var textBody = string.Empty;
                string repositoryUri = ((dynamic)data).RepositoryUri;
                string repositoryManagerUri = repositoryUri.Replace(repositoryUri.Substring(repositoryUri.LastIndexOf('/')), "/InRuleCatalogManager"); //, repositoryUri.LastIndexOf('/') - 1)), "/InRuleCatalogManager");

                if (map.ContainsKey("OperationName"))
                    textBody = $"*{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}*\n";

                textBody += $"*Catalog:* {((dynamic)data).RepositoryUri}\n";

                textBody += $"*Catalog Manager (likely location):* {repositoryManagerUri}\n";

                if (map.ContainsKey("Name"))
                    textBody += $"*Rule application:* {((dynamic)data).Name}\n";

                if (map.ContainsKey("RuleAppRevision"))
                    textBody += $"*Revision:* {((dynamic)data).RuleAppRevision}\n";

                if (map.ContainsKey("Label"))
                    textBody += $"*Label:* { ((dynamic)data).Label}\n";

                if (map.ContainsKey("Comment"))
                    textBody += $"*Comment:* { ((dynamic)data).Comment}\n";


                //SlackHelper.PostMessageHeaderBody($"{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}", textBody);
                SlackHelper.PostMarkdownMessage(textBody, messagePreffix);
            }
            catch (Exception ex)
            {
                WriteError($"Error writing {eventType} event out to Slack: {ex.Message}", "PUBLISH TO SLACK - ");
            }
        }

        public static async Task WriteToEmailAsync(string eventType, object data, string subjectSuffix, string htmlContent = "")
        {
            try
            {
                var map = data as IDictionary<string, object>;

                if (htmlContent == "")
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<table>");

                    if (map.ContainsKey("OperationName"))
                        sb.Append($"<tr><td><b>{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}\n");
                    //textBody = $"*{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}*\n";

                    var repositoryUri = ((dynamic)data).RepositoryUri;
                    //textBody += $"*Catalog:* {((dynamic)data).RepositoryUri}\n";
                    sb.Append($"<tr><td><b>Catalog:</b> <a href='{repositoryUri}'>{repositoryUri}</a></td></tr>");

                    string repositoryManagerUri = repositoryUri.Replace(repositoryUri.Substring(repositoryUri.LastIndexOf('/')), "/InRuleCatalogManager"); //, repositoryUri.LastIndexOf('/') - 1)), "/InRuleCatalogManager");
                    sb.Append($"<tr><td><b>Catalog Manager (likely location):</b> <a href='{repositoryManagerUri}'>{repositoryManagerUri}</a></td></tr>");

                    if (map.ContainsKey("Name"))
                        sb.Append($"<tr><td><b>Rule application:</b> {((dynamic)data).Name}</td></tr>");
                    //textBody += $"*Rule application:* {((dynamic)data).Name}\n";

                    if (map.ContainsKey("RuleAppRevision"))
                        sb.Append($"<tr><td><b>Revision:</b> {((dynamic)data).RuleAppRevision}</td></tr>");
                    //textBody += $"*Revision:* {((dynamic)data).RuleAppRevision}\n";

                    if (map.ContainsKey("Label"))
                        sb.Append($"<tr><td><b>Label:</b> { ((dynamic)data).Label}</td></tr>");
                    //textBody += $"*Label:* { ((dynamic)data).Label}\n";

                    if (map.ContainsKey("Comment"))
                        sb.Append($"<tr><td><b>Comment:</b> { ((dynamic)data).Comment}</td></tr>"); ;
                    //textBody += $"*Comment:* { ((dynamic)data).Comment}\n";

                    sb.Append("</table>");
                    htmlContent = sb.ToString();
                }

                //await SendGridHelper.SendEmail("mdrumea@inrule.com", "Project CI/CD", "mdrumea@gmail.com", "Marian Drumea (Gmail)", $"InRule.Repository.{eventType}", $"{JsonConvert.SerializeObject(data)}", htmlContent);
                await SendGridHelper.SendEmail($"{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}{subjectSuffix}", string.Empty, htmlContent);
            }
            catch (Exception ex)
            {
                WriteError($"Error writing {eventType} event out to email: {ex.Message}", "PUBLISH TO EMAIL - ");
            }
        }

        public static void WriteError(string message, string preffix)
        {
            try
            {
                SlackHelper.PostMarkdownMessage(message, preffix + "ERROR - ");
                Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                //EventLog.WriteEntry("Application", message + "\r\n\r\n" + ex.Message, EventLogEntryType.Error);
            }
            //EventLog.WriteEntry("Application", message, EventLogEntryType.Error);
        }

        public static void WriteToDevOpsPipeline(string eventType, object data)
        {
            try
            {
                AzureDevOpsApiHelper.QueuePipelineBuild();
            }
            catch (Exception ex)
            {
                WriteError($"Error writing {eventType} event Pipeline: {ex.Message}\r\n" + JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented), "PUBLISH TO DEVOPS PIPELINE - ");
            }
        }

        public static void WriteToEventGrid(string eventType, object data)
        {
            try
            {
                string EventGridTopicEndpoint = SettingsManager.Get("EventGridTopicEndpoint");
                string EventGridTopicKey = SettingsManager.Get("EventGridTopicKey");

                if (!string.IsNullOrEmpty(EventGridTopicKey) && !string.IsNullOrEmpty(EventGridTopicEndpoint))
                {
                    SlackHelper.PostSimpleMessage($"Publish Event to Azure Event Grid Topic Endpoint {EventGridTopicEndpoint}", "PUBLISH TO EVENT GRID - ");
                    using (var client = new EventGridClient(new TopicCredentials(EventGridTopicKey)))
                    {
                        var events = new List<EventGridEvent>()
                        {
                            //https://docs.microsoft.com/en-us/dotnet/architecture/serverless/event-grid
                            new EventGridEvent()
                            {
                                Id = Guid.NewGuid().ToString(),
                                EventType = $"InRule.Repository.{eventType}",
                                Subject = eventType, //TODO: Consider including a config for EnvironmentName to include in the Subject
                                Data = data,
                                EventTime = ((dynamic)data).UtcTimestamp,
                                DataVersion = "2.0"
                            }
                        };

                        client.PublishEventsAsync(new Uri(EventGridTopicEndpoint).Host, events).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError($"Error writing {eventType} event out to Event Grid: {ex.Message}", "PUBLISH TO EVENT GRID - ");
            }
        }
    }
}