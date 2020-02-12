using System;
using System.Collections.Generic;
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
                ILogger logger = new ULSLogger("Test.Activities.Automation.Test");

                logger?.LogInformation("Timer executing");

                try
                {
                    var activityService = new FetchActivityService(logger);

                    var activities = activityService.FetchActivities();

                    var spActivityService=new SyncActivityService(logger);

                    spActivityService.SyncActivities(activities);
                }
                catch (Exception e)
                {
                    logger?.LogError(e.Message);
                }

                logger?.LogInformation("Timer has executed");
            }
            catch
            {
            }
        }
    }
}
