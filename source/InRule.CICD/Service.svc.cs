using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using InRule.Authoring.BusinessLanguage;
using InRule.Repository;
using System.ServiceModel.Activation;
using System.Xml.Serialization;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InRule.Repository.Client;
using Newtonsoft.Json.Converters;
using InRule.CICD.Helpers;
using System.Web.Script.Serialization;

namespace InRule.CICD
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class Service : IService
    {
        //public IAsyncResult BeginGetRuleAppReport(string ruleAppXml, AsyncCallback callback, object asyncState)
        public Stream GetRuleAppReport(Stream data)
        {
            string fileFullPath = GetRuleAppReportFile(data);
            Encoding localEncoding = Encoding.UTF8;

            MemoryStream mem = new MemoryStream(System.IO.File.ReadAllBytes(fileFullPath));
            string reportContent = localEncoding.GetString(mem.ToArray());
            mem.Dispose();

            return new MemoryStream(Encoding.UTF8.GetBytes(reportContent));
        }

        public string GetRuleAppReportToGitHub(Stream data)
        {
            string fileName = "testgithubreport" + System.Guid.NewGuid().ToString();
            string fileFullPath = GetRuleAppReportFile(data);
            Encoding localEncoding = Encoding.UTF8;

            MemoryStream mem = new MemoryStream(System.IO.File.ReadAllBytes(fileFullPath));
            string reportContent = localEncoding.GetString(mem.ToArray());
            mem.Dispose();

            var downloadGitHubLink = GitHubHelper.UploadFileToRepo(reportContent, fileName + ".htm").Result;
            //SlackHelper.PostMessageWithDownloadButton("Click here to download rule application report from GitHub", fileName, downloadGitHubLink, "RULEAPP REPORT - ");

            return downloadGitHubLink;
        }

        private string GetRuleAppReportFile(Stream data)
        {
            StreamReader reader = new StreamReader(data);
            string ruleAppXml = reader.ReadToEnd();

            XmlDocument ruleAppDoc = new XmlDocument();
            ruleAppDoc.LoadXml(ruleAppXml);
            var ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);

            Encoding localEncoding = Encoding.UTF8;
            TemplateEngine templateEngine = new TemplateEngine();
            templateEngine.LoadRuleApplication(ruleAppDef);
            templateEngine.LoadStandardTemplateCatalog();
            FileInfo fileInfo = InRule.Authoring.Reporting.RuleAppReport.RunRuleAppReport(ruleAppDef, templateEngine);
            templateEngine.Dispose();

            return fileInfo.FullName;
        }

        //public string EndGetRuleAppReport(IAsyncResult r)
        //{
        //    CompletedAsyncResult<string> result = r as CompletedAsyncResult<string>;
        //    Console.WriteLine("EndServiceAsyncMethod called with: \"{0}\"", result.Data);
        //    return result.Data;
        //}

        public Stream GetRuleAppDiffReport(Stream data)
        {
            string fileFullPath = GetRuleAppDiffReportFile(data);
            Encoding localEncoding = Encoding.UTF8;

            MemoryStream mem = new MemoryStream(System.IO.File.ReadAllBytes(fileFullPath));
            string reportContent = localEncoding.GetString(mem.ToArray());
            mem.Dispose();

            return new MemoryStream(Encoding.UTF8.GetBytes(reportContent));
        }

        public string GetRuleAppDiffReportToGitHub(Stream data)
        {
            string fileName = "testgithubdiffreport" + System.Guid.NewGuid().ToString();
            string fileFullPath = GetRuleAppDiffReportFile(data);
            Encoding localEncoding = Encoding.UTF8;

            MemoryStream mem = new MemoryStream(System.IO.File.ReadAllBytes(fileFullPath));
            string reportContent = localEncoding.GetString(mem.ToArray());
            mem.Dispose();

            var downloadGitHubLink = GitHubHelper.UploadFileToRepo(reportContent, fileName + ".htm").Result;
            //SlackHelper.PostMessageWithDownloadButton("Click here to download rule application difference report from GitHub", fileName, downloadGitHubLink, "RULEAPP DIFF REPORT - ");

            return downloadGitHubLink;
        }

        private string GetRuleAppDiffReportFile(Stream data)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PostData));

            StreamReader reader = new StreamReader(data);

            PostData diffReportRequest = (PostData)serializer.Deserialize(data);

            string fromRuleAppXml = diffReportRequest.FromRuleAppXml;
            string toRuleAppXml = diffReportRequest.ToRuleAppXml;

            XmlDocument ruleAppDoc = new XmlDocument();
            ruleAppDoc.LoadXml(fromRuleAppXml);
            XmlNode defTag = ruleAppDoc.GetElementsByTagName("RuleApplicationDef")[0];
            var ruleAppDef = RuleApplicationDef.LoadXml(fromRuleAppXml);

            XmlDocument toRuleAppDoc = new XmlDocument();
            toRuleAppDoc.LoadXml(fromRuleAppXml);
            XmlNode toDefTag = toRuleAppDoc.GetElementsByTagName("RuleApplicationDef")[0];
            var toRuleAppDef = RuleApplicationDef.LoadXml(toRuleAppXml);

            Encoding localEncoding = Encoding.UTF8;
            TemplateEngine templateEngine = new TemplateEngine();
            templateEngine.LoadRuleApplication(ruleAppDef);
            templateEngine.LoadRuleApplication(toRuleAppDef);
            templateEngine.LoadStandardTemplateCatalog();
            FileInfo fileInfo = InRule.Authoring.Reporting.DiffReport.CreateReport(ruleAppDef, toRuleAppDef);
            templateEngine.Dispose();

            return fileInfo.FullName;
        }

        [Obsolete]
        public string ApproveRuleAppPromotion(string data)
        {
            string label = string.Empty;
            string ruleAppGuid = string.Empty;
            string repositoryUri = string.Empty;
            string revision = string.Empty;
            try
            {
                data = data.Replace(" ", "+");

                var eventDataString = CryptoHelper.DecryptString(string.Empty, data);

                //var eventData = (IDictionary<string, object>)((dynamic)eventDataString); //JsonConvert.DeserializeObject<ExpandoObject>(eventDataString);

                var obj = JsonConvert.DeserializeObject(eventDataString);
                var jObj = obj as JArray;

                var eventData = JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(eventDataString);

                foreach (var item in jObj)
                {
                    if (item["Key"].ToString() == "Label")
                        label = item["Value"].ToString();

                    if (item["Key"].ToString() == "RuleAppRevision")
                        revision = item["Value"].ToString();

                    if (item["Key"].ToString() == "RepositoryUri")
                        repositoryUri = item["Value"].ToString();

                    if (item["Key"].ToString() == "GUID")
                        ruleAppGuid = item["Value"].ToString();

                }

                using (RuleCatalogConnection connection = new RuleCatalogConnection(new Uri(repositoryUri), new TimeSpan(0, 10, 0), SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword"))) // SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword"));
                {
                    var ruleAppDef = connection.GetSpecificRuleAppRevision(new Guid(ruleAppGuid), int.Parse(revision));

                    connection.ApplyLabel(ruleAppDef, label);
                }
            }
            catch (Exception ex)
            {
                return "ERROR APPLYING LABEL: " + ex.Message;
            }
            return "SUCCESS!";
            //return jObj[0]["Key"].ToString() + " " + jObj[0]["Value"].ToString();
        }

        public Stream ProcessInRuleEvent(Stream data)
        {
            StreamReader reader = new StreamReader(data);
            string request = reader.ReadToEnd();
            dynamic eventDataSource = JsonConvert.DeserializeObject<ExpandoObject>(request, new ExpandoObjectConverter());

            try
            {
                HandleAfterCallAsync(eventDataSource);
            }
            catch(Exception ex)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(ex.Message));
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(request));
        }

        public string ProcessInRuleEventI(string data)
        {
            data = data.Replace(" ", "+");
            var eventDataString = CryptoHelper.DecryptString("", data).Replace("InRule CI/CD - ", "");

            var jsonDeserialized = new JavaScriptSerializer().Deserialize<IEnumerable<IDictionary<string, object>>>(eventDataString);
            var eventData = go(jsonDeserialized);

            try
            {
                HandleAfterCallAsync(eventData);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return eventDataString;
        }

        public ExpandoObject go(IEnumerable<IDictionary<string, object>> lst)
        {

            return lst.Aggregate(new ExpandoObject(),
                                      (aTotal, n) => {
                                          (aTotal as IDictionary<string, object>).Add(n["Key"].ToString(), n["Value"] is object[]? go(((object[])n["Value"]).Cast<IDictionary<string, Object>>()) : n["Value"]);
                                          return aTotal;
                                      });

        }

        private void HandleAfterCallAsync(ExpandoObject eventDataSource) //, object returnValue)
        {
            //var processingTask = Task.Run(() =>
            //{
                try
                {
                //dynamic eventData = new ExpandoObject();
                //var d = eventData as IDictionary<string, object>;

                string FilterByUser = SettingsManager.Get("FilterEventsByUser").ToLower();
                //string InRuleCICDServiceUri = SettingsManager.Get("InRuleCICDServiceUri");
                string ApplyLabelApprover = SettingsManager.Get("ApprovalFlow.ApplyLabelApprover");
                //var eventData = (dynamic)JsonConvert.SerializeObject(eventDataSource);
                var eventData = (dynamic)eventDataSource;
                //if (eventData.RequestorUsername.ToString().ToLower() != FilterByUser)
                //    return;

                var filterByUsers = FilterByUser.Split(' ').ToList();

                if (!filterByUsers.Contains(eventData.RequestorUsername.ToString().ToLower()))
                    return;

                eventData.ProcessingTimeInMs = (DateTime.UtcNow - ((DateTime)eventData.UtcTimestamp)).TotalMilliseconds;

                #region Try to retrieve the RuleApp Name from RuleAppXml or maintenance action result information
                //string ruleAppXml = null;
                //if (returnValue is CheckinRuleAppResponse checkinResponse)
                //    ruleAppXml = checkinResponse.RuleAppXml.Xml;
                //else if (returnValue is CreateRuleAppResponse createResponse)
                //    ruleAppXml = createResponse.AppXml.Xml;
                //else if (returnValue is DeleteRuleAppResponse deleteResponse)
                //    ruleAppXml = deleteResponse.RuleAppXml.Xml;
                //else if (returnValue is DeleteWorkspaceResponse deleteWorkspaceResponse)
                //    ruleAppXml = deleteWorkspaceResponse.RuleAppXml.Xml;
                //else if (returnValue is OverwriteRuleAppResponse overwriteResponse)
                //    ruleAppXml = overwriteResponse.AppXml.Xml;
                //else if (returnValue is PromoteRuleAppResponse promoteResponse)
                //    ruleAppXml = promoteResponse.AppXml.Xml;
                //else if (returnValue is RepairCatalogResponse repairResponse)
                //    eventData.ResultData = JsonConvert.SerializeObject(repairResponse.Info);
                //else if (returnValue is RunDiagnosticsResponse diagnosticResponse)
                //    eventData.ResultData = JsonConvert.SerializeObject(diagnosticResponse.Info);
                //else if (returnValue is SaveRuleAppResponse saveResponse)
                //    ruleAppXml = saveResponse.RuleAppXml.Xml;
                //else if (returnValue is UndoCheckoutResponse undoCheckoutResponse)
                //    ruleAppXml = undoCheckoutResponse.RuleAppXml.Xml;
                //else if (returnValue is UpdateStaleDefsResponse updateStaleDefsResponse)
                //    ruleAppXml = updateStaleDefsResponse.AppXml.Xml;
                //else if (returnValue is UpgradeCatalogRuleAppSchemaVersionResponse upgradeResponse)
                //    eventData.ResultData = JsonConvert.SerializeObject(upgradeResponse.Info);
                //else if (returnValue is UpgradeStatusResponse upgradeStatusResponse)
                //    eventData.ResultData = JsonConvert.SerializeObject(upgradeStatusResponse.Status);

                //if (ruleAppXml != null)
                //    LoadRuleAppNameFromXml(eventData, ruleAppXml);
                #endregion

                InRuleEventHelper.ProcessEventAsync(eventData, string.Empty).Wait();
                return;

                #region commented helpers actions
                //// Output final event to desired listener(s)
                //var eventDataJson = JsonConvert.SerializeObject(eventData);

                ////eventData.RevisionComments = connection.GetCheckinHistoryForDef(ruleAppDef.Guid).Values.OfType<CheckinInfo>().FirstOrDefault().Comment;

                //if (eventData.OperationName == "CheckinRuleApp" || eventData.OperationName == "OverwriteRuleApp" || eventData.OperationName == "CreateRuleApp")
                //{
                //    //var ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);

                //    RuleCatalogConnection connection = new RuleCatalogConnection(new Uri(eventData.RepositoryUri), new TimeSpan(0, 10, 0), SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword"));
                //    var ruleAppDef = connection.GetSpecificRuleAppRevision(new System.Guid(eventData.GUID.ToString()), int.Parse(eventData.RuleAppRevision.ToString()));
                //    //eventData.RuleAppRevision = ruleAppDef.Revision;

                //    //AzureServiceBusHelper.SendMessageAsync(eventDataJson);

                //    PublishEventHelper.WriteToSlack(eventData.OperationName, eventData, "CATALOG EVENT - ");
                //    PublishEventHelper.WriteToEmailAsync(eventData.OperationName, eventData, " -");

                //    InRuleReportingHelper.GetRuleAppReportAsync(eventData.OperationName, eventData, ruleAppDef, false, true, true);

                //    if (ruleAppDef.Revision > 1)
                //    {
                //        var fromRuleAppDef = connection.GetSpecificRuleAppRevision(ruleAppDef.Guid, ruleAppDef.Revision - 1);
                //        InRuleReportingHelper.GetRuleAppDiffReportAsync(eventData.OperationName, eventData, fromRuleAppDef, ruleAppDef, false, true, true);
                //    }

                //    //var comments = string.Empty;
                //    //foreach(var checkIn in connection.GetCheckinHistoryForDef(ruleAppDef.Guid).Values)
                //    //{
                //    //    comments += checkIn.Comment + "\r\n";
                //    //}

                //    //eventData.RevisionComments = comments;

                //    PublishEventHelper.WriteToEventGrid(eventData.OperationName, eventData);

                //    PublishEventHelper.WriteToDevOpsPipeline(eventData.OperationName, eventData);

                //    JavaDistributionHelper.GenerateJavaJar(ruleAppDef, true, false, true);

                //    try
                //    {
                //        JavaScriptDistributionHelper.CallDistributionServiceAsync(ruleAppDef, true, false, true);
                //    }
                //    catch (Exception) { }

                //    TestSuiteRunnerHelper.RunRegressionTestsAsync(eventData.OperationName, eventData, ruleAppDef);
                //}
                //else if(eventData.OperationName == "ApplyLabel")
                //{
                //    PublishEventHelper.WriteToSlack(eventData.OperationName, eventData, "CATALOG EVENT - ");
                //    PublishEventHelper.WriteToEmailAsync(eventData.OperationName, eventData, " -");

                //    if (eventData.RequestorUsername.ToString().ToLower() != ApplyLabelApprover.ToLower())
                //    {
                //        CheckInApprovalHelper.SendApproveRequest(eventDataSource);
                //    }
                //}
                //else
                //{
                //    PublishEventHelper.WriteToSlack(eventData.OperationName, eventData, "CATALOG EVENT - ");
                //    PublishEventHelper.WriteToEmailAsync(eventData.OperationName, eventData, string.Empty);
                //}

                ////PublishEventToAppInsights(eventData.OperationName, eventData);
                //WriteEventToEventLog(eventData.OperationName, eventData);
                //WriteEventToDatabase(eventData.OperationName, eventData);
                #endregion
            }
            catch (Exception ex)
            {
                NotificationHelper.NotifyAsync("Error processing data in AfterCall: " + ex.Message, "AFTER CALL EVENT - ", "Debug").Wait();
            }
            //});
        }
        private void LoadRuleAppNameFromXml(ExpandoObject eventData, string ruleAppXml)
        {
            try
            {
                XmlDocument ruleAppDoc = new XmlDocument();
                ruleAppDoc.LoadXml(ruleAppXml);
                XmlNode defTag = ruleAppDoc.GetElementsByTagName("RuleApplicationDef")[0];
                ((dynamic)eventData).Name = defTag.Attributes["Name"].Value;
            }
            catch (Exception ex)
            {
                // Lighter-weight log, because this isn't that significant
                Console.WriteLine("Error retrieving RuleApplicationDef Name attribute: " + ex.Message);
            }
        }

        public Stream RunTestsInGitHubForRuleapp(Stream ruleAppXmlStream)
        {
            StreamReader reader = new StreamReader(ruleAppXmlStream);
            string ruleAppXml = reader.ReadToEnd();
            var ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);

            TestSuiteRunnerHelper.RunRegressionTestsAsync(ruleAppDef).Wait();

            var reportContent = "Success.";
            return new MemoryStream(Encoding.UTF8.GetBytes(reportContent));
        }
    }
}
