using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace Test.Activities.Automation.TimerJob.Features.TimerJobFeature
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("353e1448-874f-405e-af35-3a78c37e98af")]
    public class TimerJobFeatureEventReceiver : SPFeatureReceiver
    {
        const string docJobName = "Test.Activities.Automation.Job";

        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            var webApp = properties.Feature.Parent as SPWebApplication;

            foreach (var job in webApp.JobDefinitions)
            {
                if (job.Name == docJobName)
                    job.Delete();
            }

            var docJob = new TimerJob(docJobName, webApp,
                SPServer.Local, SPJobLockType.Job);

            var schedule = new SPDailySchedule();
            docJob.Schedule = schedule;
            docJob.Update();
        }

        // Uncomment the method below to handle the event raised before a feature is deactivated.

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            var site = properties.Feature.Parent as SPWebApplication;

            foreach (var job in site.JobDefinitions)
            {
                if (job.Name == docJobName)
                    job.Delete();
            }
        }


        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
    }
}
