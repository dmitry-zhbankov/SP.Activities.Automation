using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Activation;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib;
using Test.Activities.Automation.ActivityLib.Models;

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

        public HttpStatusCode FillActivities(IEnumerable<ActivityInfo> activities)
        {
            var statusCode = HttpStatusCode.Accepted;

            try
            {
                _logger?.LogInformation("Request received");

                try
                {
                    var spActivityService = new SyncActivityService(_logger);

                    spActivityService.SynchronizeSpActivities(activities);

                    _logger.LogInformation("Request has been treated successfully");
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.Message);
                    statusCode = HttpStatusCode.InternalServerError;
                }

                _logger?.LogInformation("Sending response");
            }
            catch
            {
                statusCode = HttpStatusCode.InternalServerError;
            }

            return statusCode;
        }
    }
}
