using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Services;
using Test.Activities.Automation.ActivityLib.Sources;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ILogger logger = new DebugLogger();
                logger?.LogInformation("Executing main");

                try
                {
                    using (var site = new SPSite(Constants.Host))
                    using (var web = site.OpenWeb(Constants.Web))
                    {
                        logger?.LogInformation("Fetching activities");

                        var configList = web.Lists[Constants.Lists.Configurations];

                        var spListActivities = web.Lists.TryGetList(Constants.Lists.Activities);
                        if (spListActivities == null) throw new Exception("Getting SP activity list failed");

                        var spListMentors = web.Lists.TryGetList(Constants.Lists.Mentors);
                        if (spListMentors == null) throw new Exception("Getting SP mentors list failed");

                        var spListRootMentors = web.Lists.TryGetList(Constants.Lists.RootMentors);
                        if (spListRootMentors == null) throw new Exception("Getting SP root mentor list failed");

                        var now = DateTime.Now;
                        var minDate = new DateTime(now.Year, now.AddMonths(-1).Month, 1);
                        var maxDate = now.Date;

                        SpMember.SetLogger(logger);
                        var spMembers = SpMember.GetSpMembers(spListMentors, spListRootMentors);

                        var activitySourceList = new List<ActivitySource>()
                        {
                            new GitLabActivitySource(logger,spMembers,configList),
                            new SPCalendarActivitySource(logger,web)
                        };
                        var activities = activitySourceList.SelectMany(x => x.FetchActivities());

                        SpActivity.SetLogger(logger);
                        var spActivities = SpActivity.GetSpActivities(spListActivities, spListMentors, spListRootMentors, minDate, maxDate);

                        var ensureService = new EnsureService(logger);
                        var itemsToUpdate = ensureService.Ensure(spActivities, activities, spMembers);

                        web.AllowUnsafeUpdates = true;
                        SpActivity.UpdateSpActivities(itemsToUpdate, spListActivities);
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError(e.Message);
                }

                logger?.LogInformation("Main has been executed");
            }
            catch (Exception e)
            {
            }

            Console.ReadKey();
        }
    }
}
