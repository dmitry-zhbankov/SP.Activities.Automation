using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Test.Activities.Automation.ActivityLib.Helpers;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Sources;
using Test.Activities.Automation.ActivityLib.Utils.Constants;
using SPMember = Test.Activities.Automation.ActivityLib.Models.SPMember;

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

        public override async void Execute(Guid targetInstanceId)
        {
            try
            {
                _logger?.LogInformation("Timer executing");

                try
                {
                    using (var site = new SPSite(Constants.Host))
                    using (var web = site.OpenWeb(Constants.Web))
                    {
                        _logger?.LogInformation("Fetching activities");

                        _logger?.LogInformation("Getting existing members from SP");
                        var spMembers = SPMember.GetSPMembers(web);

                        var activitySourceList = new List<ActivitySource>()
                        {
                            new GitLabActivitySource(_logger, spMembers, web),
                            new SPCalendarActivitySource(_logger, spMembers, web),
                        };
                        var activities = activitySourceList.SelectMany(x => x.FetchActivities()).ToList();

                        await SendActivities(activities);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.Message);
                }

                _logger?.LogInformation("Timer has been executed");
            }
            catch
            {
            }
        }

        private async Task SendActivities(IEnumerable<ActivityInfo> activities)
        {
            try
            {
                _logger?.LogInformation("Sending activities to service");

                var str = APIHelper.JsonSerialize(activities);
                var uri = new Uri(Constants.ServiceUrl);

                for (var i = 0; i < RequestAttempts; i++)
                {
                    using (var response = await APIHelper.PostJsonAsync(uri, str))
                    {
                        if (response.IsSuccessStatusCode) return;

                        _logger?.LogWarning(
                            $"Request {i} failed. Response='{await response.Content.ReadAsStringAsync()}'");
                    }

                    if (i != RequestAttempts)
                    {
                        await Task.Delay(new TimeSpan(0, 5, 0));
                    }
                }

                throw new Exception("All requests to service failed");
            }
            catch (Exception e)
            {
                throw new Exception($"Sending activities to service failed. {e}");
            }
        }
    }
}
