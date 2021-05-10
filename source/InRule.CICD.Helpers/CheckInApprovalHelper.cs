using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace InRule.CICD.Helpers
{
    public class CheckInApprovalHelper
    {
        private static readonly string moniker = "ApprovalFlow";

        public static async Task SendApproveRequestAsync(ExpandoObject eventDataSource)
        {
            await SendApproveRequestAsync(eventDataSource, moniker);
        }

        public static async Task SendApproveRequestAsync(ExpandoObject eventDataSource, string moniker)
        {
            string InRuleCICDServiceUri = SettingsManager.Get("InRuleCICDServiceUri");
            string ApplyLabelApprover = SettingsManager.Get($"{moniker}.ApplyLabelApprover");
            string NotificationChannel = SettingsManager.Get($"{moniker}.NotificationChannel");
            
            try
            {
                var eventData = (dynamic)eventDataSource;

                if (eventData.RequestorUsername.ToString().ToLower() != ApplyLabelApprover.ToLower())
                {

                    JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                    string applyLabelEvent = javaScriptSerializer.Serialize(eventData);
                    string encryptedApplyLabelEvent = CryptoHelper.EncryptString(string.Empty, applyLabelEvent);

                    var approvalUrl = $"{InRuleCICDServiceUri + "/ApproveRuleAppPromotion"}?data={encryptedApplyLabelEvent}";

                    var channels = NotificationChannel.Split(' ');
                    foreach (var channel in channels)
                    {

                        switch (SettingsManager.GetHandlerType(channel))
                        {
                            case IHelper.InRuleEventHelperType.Teams:
                                TeamsHelper.PostMessageWithDownloadButton($"Click here to approve label {eventData.Label} for rule application {eventData.GUID}", "Apply Label", approvalUrl, "APPROVAL FLOW - ", channel);
                                break;
                            case IHelper.InRuleEventHelperType.Slack:
                                SlackHelper.PostMessageWithDownloadButton($"Click here to approve label {eventData.Label} for rule application {eventData.GUID}", "Apply Label", approvalUrl, "APPROVAL FLOW - ", channel);
                                break;
                            case IHelper.InRuleEventHelperType.Email:
                                await SendGridHelper.SendEmail("APPROVE AND APPLY LABEL", "", $"Click <a href='{approvalUrl}'>here</a> to approve", channel);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Error sending apply label approval request: {ex.Message}", "APPROVAL FLOW", "Debug");
            }
        }
    }
}
