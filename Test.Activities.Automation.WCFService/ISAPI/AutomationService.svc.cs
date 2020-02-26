using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Services;
using SPMember = Test.Activities.Automation.ActivityLib.Models.SPMember;

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

                    var web = SPContext.Current.Web;

                    _logger?.LogInformation("Getting existing members from SP");
                    var spMembers = SPMember.GetSPMembers(web);

                    var now = DateTime.Now;
                    var minDate = new DateTime(now.Year, now.AddMonths(-1).Month, 1);
                    var maxDate = now.Date;

                    _logger?.LogInformation("Getting existing activities from SP");
                    var spActivities = SPActivity.GetSPActivities(web, spMembers, minDate, maxDate);

                    var ensureService = new EnsureService(_logger);
                    var itemsToUpdate = ensureService.Ensure(spActivities, activities, spMembers);

                    var propAllowUpdates = web.AllowUnsafeUpdates;
                    try
                    {
                        _logger?.LogInformation("Updating SP activities list");
                        web.AllowUnsafeUpdates = true;
                        SPActivity.UpdateSPActivities(itemsToUpdate, web);
                    }
                    finally
                    {
                        web.AllowUnsafeUpdates = propAllowUpdates;
                    }

                    _logger.LogInformation("Request has been treated successfully");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Synchronizing activities failed. {e}");

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
