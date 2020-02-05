using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Activation;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib;

namespace Test.Activities.Automation.WCFService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AutomationService : IAutomationService
    {
        public HttpStatusCode FillActivities(IEnumerable<ActivityInfo> activities)
        {
            HttpStatusCode statusCode;

            using (var site = new SPSite(Constants.Host))
            using (var web = site.OpenWeb(Constants.Web))
            {
                var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spList == null)
                {
                    return HttpStatusCode.InternalServerError;
                }

                var spActivities = GetSpActivities(spList);

                var newSpActivities = GetNewSpActivities(activities, spActivities, web);

                web.AllowUnsafeUpdates = true;
                statusCode = UpdateSpActivities(newSpActivities, spList);
            }

            return statusCode;
        }

        private static HttpStatusCode UpdateSpActivities(IEnumerable<SpActivity> newSpActivities, SPList spList)
        {
            var statusCode = HttpStatusCode.Accepted;

            foreach (var item in newSpActivities)
            {
                if (item.IsNew)
                {
                    var newItem = spList.Items.Add();

                    newItem[Constants.Activity.User] = item.User;
                    newItem[Constants.Activity.Month] = item.Month;
                    newItem[Constants.Activity.Year] = item.Year;
                    var newActivitiesValue = new SPFieldMultiChoiceValue();
                    foreach (var itemActivity in item.Activities)
                    {
                        newActivitiesValue.Add(itemActivity);
                    }

                    newItem[Constants.Activity.Activities] = newActivitiesValue;

                    newItem.Update();

                    statusCode = HttpStatusCode.Created;

                    continue;
                }

                if (!item.IsNewActivity) continue;

                var itemToUpdate = spList.Items.GetItemById(item.Id);
                var activityField = itemToUpdate.Fields.GetField(Constants.Activity.Activities);
                var activityFieldValue =
                    activityField.GetFieldValue(itemToUpdate[Constants.Activity.Activities].ToString()) as
                        SPFieldMultiChoiceValue;
                for (int i = 0; i < activityFieldValue.Count; i++)
                {
                    item.Activities.Remove(activityFieldValue[i]);
                }

                foreach (var newItemActivity in item.Activities)
                {
                    activityFieldValue.Add(newItemActivity);
                }
                activityField.Update();

                statusCode = HttpStatusCode.Created;
            }

            return statusCode;
        }

        private static IEnumerable<SpActivity> GetNewSpActivities(IEnumerable<ActivityInfo> activities, IEnumerable<SpActivity> spActivities, SPWeb web)
        {
            var newSpActivities = new List<SpActivity>();

            foreach (var item in activities)
            {
                var spActivity = spActivities.FirstOrDefault(x =>
                    x.Year == item.Date.Year && x.Month == item.Date.Month && x.User.Email == item.UserEmail);
                if (spActivity != null)
                {
                    if (!spActivity.Activities.Contains(item.Activity))
                    {
                        var newSpActivity = new SpActivity()
                        {
                            Id = spActivity.Id,
                            Month = spActivity.Month,
                            User = spActivity.User,
                            Year = spActivity.Year,
                            Activities = new List<string>(spActivity.Activities),
                            IsNewActivity = true
                        };

                        newSpActivity.Activities.Add(item.Activity);
                    }
                }
                else
                {
                    var act = newSpActivities.FirstOrDefault(x => x.User.Email == item.UserEmail);
                    if (act != null)
                    {
                        act.Activities.Add(item.Activity);
                        continue;
                    }

                    try
                    {
                        var user = web.Users.GetByEmail(item.UserEmail);

                        newSpActivities.Add(new SpActivity()
                        {
                            Month = item.Date.Month,
                            Year = item.Date.Year,
                            User = user,
                            Activities = new List<string>()
                            {
                                item.Activity
                            },
                            IsNew = true
                        });
                    }
                    catch
                    {
                    }
                }
            }

            return newSpActivities;
        }

        private static IEnumerable<SpActivity> GetSpActivities(SPList spList)
        {
            var spActivities = new List<SpActivity>();
            foreach (var item in spList.GetItems().Cast<SPListItem>())
            {
                var userField = item.Fields.GetField(Constants.Activity.User);
                var userFieldValue = userField.GetFieldValue(item[Constants.Activity.User].ToString()) as SPFieldUserValue;
                var user = userFieldValue.User;

                var month = Convert.ToInt32(item[Constants.Activity.Month]);
                var year = Convert.ToInt32(item[Constants.Activity.Year]);

                var activityField = item.Fields.GetField(Constants.Activity.Activities);
                var activityFieldValue =
                    activityField.GetFieldValue(item[Constants.Activity.Activities].ToString()) as SPFieldMultiChoiceValue;

                var activs = new List<string>();
                for (int i = 0; i < activityFieldValue.Count; i++)
                {
                    activs.Add(activityFieldValue[i]);
                }

                spActivities.Add(new SpActivity()
                {
                    User = user,
                    Month = month,
                    Year = year,
                    Activities = activs,
                    Id = item.ID
                });
            }

            return spActivities;
        }
    }

    public class SpActivity
    {
        public int Id { get; set; }
        public SPUser User { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public List<string> Activities { get; set; }
        public bool IsNew { get; set; }
        public bool IsNewActivity { get; set; }
    }
}
