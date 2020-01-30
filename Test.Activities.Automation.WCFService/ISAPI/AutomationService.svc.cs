using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Activation;
using Test.Activities.Automation.ActivityLib;

namespace Test.Activities.Automation.WCFService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AutomationService : IAutomationService
    {
        public bool FillActivities(ActivityInfo[] activities)
        {
            return false;
        }
    }
}
