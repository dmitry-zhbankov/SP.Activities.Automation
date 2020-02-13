using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Models.Activity.Services.SyncActivityService;

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
                    var activityService = new FetchActivityService(logger);

                    var activities = activityService.FetchActivities();

                    var spActivityService = new SyncActivityService(logger);

                    spActivityService.SyncActivities(activities);
                }
                catch (Exception e)
                {
                    logger?.LogError(e.Message);
                }

                logger?.LogInformation("Main has executed");
            }
            catch (Exception e)
            {
            }
        }
    }
}
