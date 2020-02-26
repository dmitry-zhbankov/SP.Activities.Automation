using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Services;
using Test.Activities.Automation.ActivityLib.Sources;
using Test.Activities.Automation.ActivityLib.Utils.Constants;
using SPMember = Test.Activities.Automation.ActivityLib.Models.SPMember;

namespace Test.Activities.Automation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new DebugLogger();

            try
            {
                logger?.LogInformation("Executing main");

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    logger?.LogInformation("Fetching activities");
                    
                    var now = DateTime.Now;
                    var minDate = new DateTime(now.Year, now.AddMonths(-1).Month, 1);
                    var maxDate = now.Date;

                    logger?.LogInformation("Getting existing members from SP");
                    var spMembers = SPMember.GetSPMembers(web);

                    var activitySourceList = new List<ActivitySource>()
                    {
                        new GitLabActivitySource(logger, spMembers, web),
                        new SPCalendarActivitySource(logger, spMembers, web),
                    };
                    var activities = activitySourceList.SelectMany(x => x.FetchActivities()).ToList();

                    logger?.LogInformation("Synchronizing activities");
                    logger?.LogInformation("Getting existing activities from SP");
                    var spActivities = SPActivity.GetSPActivities(web, spMembers, minDate, maxDate);

                    var ensureService = new EnsureService(logger);
                    var itemsToUpdate = ensureService.Ensure(spActivities, activities, spMembers);

                    var propAllowUpdates = web.AllowUnsafeUpdates;
                    try
                    {
                        logger?.LogInformation("Updating SP activities list");
                        web.AllowUnsafeUpdates = true;
                        SPActivity.UpdateSPActivities(itemsToUpdate, web);
                    }
                    finally
                    {
                        web.AllowUnsafeUpdates = propAllowUpdates;
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e.Message);
            }

            logger?.LogInformation("Main has been executed");

            Console.ReadKey();
        }
    }
}
