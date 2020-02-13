using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Models.Helpers;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.TimerJob
{
    public class TimerJob : SPJobDefinition
    {
        private const int RequestAttempts = 3;

        private ILogger _logger;

        public TimerJob()
        {
            try
            {
                _logger = new ULSLogger(GetType().FullName);
            }
            catch
            {
            }
        }

        public TimerJob(string name, SPWebApplication webApp, SPServer server, SPJobLockType lockType)
            : base(name, webApp, server, lockType)
        {
        }

        public override void Execute(Guid targetInstanceId)
        {
            try
            {
                _logger?.LogInformation("Timer executing");

                try
                {
                    var activityService = new FetchActivityService(_logger);

                    var activities = activityService.FetchActivities();

                    SendActivities(activities);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.Message);
                }

                _logger?.LogInformation("Timer has executed");
            }
            catch
            {
            }
        }

        private async void SendActivities(IEnumerable<ActivityInfo> activities)
        {
            try
            {
                _logger?.LogInformation("Sending activities to service");

                var str = APIHelper.JsonSerialize(activities);
                var uri = new Uri(Constants.ServiceUrl);

                for (var i = 0; i < RequestAttempts; i++)
                {
                    if (await APIHelper.PostJsonAsync(uri, str) == HttpStatusCode.OK) return;

                    _logger?.LogWarning($"Request {i} failed");

                    if (i != RequestAttempts)
                    {
                        await Task.Delay(new TimeSpan(0, 5, 0));
                    }
                }

                throw new Exception("All requests to service failed");
            }
            catch (Exception e)
            {
                throw new Exception($"Sending activities to service failed. {e.Message}");
            }
        }
    }
}
