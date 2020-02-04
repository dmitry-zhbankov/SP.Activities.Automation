using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Activation;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib;

namespace Test.Activities.Automation.WCFService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AutomationService : IAutomationService
    {
        public bool FillActivities(IEnumerable<ActivityInfo> activities)
        {
            var spActivities = new List<SpActivity>();

            using (var site = new SPSite(Constants.Host))
            using (var web = site.OpenWeb(Constants.Web))
            {
                var spList = web.Lists.TryGetList(Constants.Lists.Activities)?.GetItems().Cast<SPListItem>();
                
                foreach (var item in spList)
                {
                    var userField = item.Fields.GetField(Constants.Activity.User);
                    var userFieldValue = userField.GetFieldValue(item[Constants.Activity.User].ToString()) as SPFieldUserValue;
                    var user = userFieldValue.User;

                    var month = Convert.ToInt32(item[Constants.Activity.Month]);
                    var year = Convert.ToInt32(item[Constants.Activity.Year]);

                    var activityField = item.Fields.GetField(Constants.Activity.Activities);
                    var activityFieldValue =
                        activityField.GetFieldValue(item[Constants.Activity.Activities].ToString()) as SPFieldMultiChoiceValue;

                    var activs=new List<string>();
                    for (int i = 0; i < activityFieldValue.Count; i++)
                    {
                        activs.Add(activityFieldValue[i]);
                    }

                    spActivities.Add(new SpActivity()
                    {
                        User=user,
                        Month=month,
                        Year = year,
                        Activities=activs
                    });
                }
            }

            foreach (var item in activities)
            {
                var act = spActivities.FirstOrDefault(x =>
                    x.Year == item.Date.Year && x.Month == item.Date.Month && x.User.Email == item.UserEmail);
                
            }

            return false;
        }
    }

    public class SpActivity
    {
        public SPUser User { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public IEnumerable<string> Activities { get; set; }
    }
}
