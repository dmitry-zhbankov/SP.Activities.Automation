using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Activities.Automation.ActivityLib.Models;

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
                    var activityService = new ActivityInfoService(logger);

                    var activities = activityService.GetActivities();

                    var spActivityService=new SpActivityService(logger);

                    spActivityService.SynchronizeSpActivities(activities);
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
