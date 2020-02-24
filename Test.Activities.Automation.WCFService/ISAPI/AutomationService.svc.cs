using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Services;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.WCFService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AutomationService : IAutomationService
    {
        private ILogger _logger;

        public AutomationService()
        {
            try
            {
                _logger = new ULSLogger(GetType().FullName);
            }
            catch
            {
            }
        }

        public void FillActivities(IEnumerable<ActivityInfo> activities)
        {
            var statusCode = HttpStatusCode.Accepted;

            var web = SPContext.Current.Web;

            try
            {
                _logger?.LogInformation("Request received");

                try
                {
                    _logger?.LogInformation("Synchronizing activities");

                    if (activities == null)
                    {
                        throw new Exception("Activities are null");
                    }

                    var now = DateTime.Now;
                    var minDate = new DateTime(now.Year, now.AddMonths(-1).Month, 1);
                    var maxDate = now.Date;

                    var spListActivities = web.Lists.TryGetList(Constants.Lists.Activities);
                    if (spListActivities == null) throw new Exception("Getting SP activity list failed");

                    var spListMentors = web.Lists.TryGetList(Constants.Lists.Mentors);
                    if (spListMentors == null) throw new Exception("Getting SP mentors list failed");

                    var spListRootMentors = web.Lists.TryGetList(Constants.Lists.RootMentors);
                    if (spListRootMentors == null) throw new Exception("Getting SP root mentor list failed");

                    SpMember.SetLogger(_logger);
                    var spMembers = SpMember.GetSpMembers(spListMentors, spListRootMentors);

                    SpActivity.SetLogger(_logger);
                    var spActivities = SpActivity.GetSpActivities(spListActivities, spMembers, minDate, maxDate);

                    var ensureService = new EnsureService(_logger);
                    var itemsToUpdate = ensureService.Ensure(spActivities, activities, spMembers);

                    web.AllowUnsafeUpdates = true;
                    SpActivity.UpdateSpActivities(itemsToUpdate, spListActivities);

                    _logger.LogInformation("Request has been treated successfully");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Synchronizing activities failed. {e.Message}");

                    statusCode = HttpStatusCode.InternalServerError;
                }

                _logger?.LogInformation("Sending response");
            }
            catch
            {
                statusCode = HttpStatusCode.InternalServerError;
            }

            var ctx = WebOperationContext.Current;
            ctx.OutgoingResponse.StatusCode = statusCode;
        }
    }
}
