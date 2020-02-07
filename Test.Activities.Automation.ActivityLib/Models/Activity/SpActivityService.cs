using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SpActivityService
    {
        private ILogger _logger;

        public SpActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public void SynchronizeSpActivities(IEnumerable<ActivityInfo> activities)
        {
            using (var site = new SPSite(Constants.Host))
            using (var web = site.OpenWeb(Constants.Web))
            {
                var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spList == null) throw new Exception("Getting SP list failed");

                var spActivityService = new SpActivityService(_logger);

                web.AllowUnsafeUpdates = true;

                var spActivities = GetSpActivities(spList);

                var newSpActivities = CalculateNewSpActivities(activities, spActivities, web);

                UpdateSpActivities(newSpActivities, spList);

                _logger.LogInformation("Request has been treated successfully");
            }
        }

        public IEnumerable<SpActivity> GetSpActivities(SPList spList)
        {
            try
            {
                _logger?.LogInformation("Getting existing activities from SP");

                var spActivities = new List<SpActivity>();

                foreach (var item in spList.GetItems().Cast<SPListItem>())
                {
                    var userField = item.Fields.GetField(Constants.Activity.User);
                    var userFieldValue =
                        userField.GetFieldValue(item[Constants.Activity.User].ToString()) as SPFieldUserValue;
                    var user = userFieldValue.User;

                    var month = Convert.ToInt32(item[Constants.Activity.Month]);
                    var year = Convert.ToInt32(item[Constants.Activity.Year]);

                    var activityField = item.Fields.GetField(Constants.Activity.Activities);
                    var activityFieldValue =
                        activityField.GetFieldValue(item[Constants.Activity.Activities].ToString()) as
                            SPFieldMultiChoiceValue;

                    var activities = new List<string>();
                    for (var i = 0; i < activityFieldValue.Count; i++) activities.Add(activityFieldValue[i]);

                    spActivities.Add(new SpActivity
                    {
                        User = user,
                        Month = month,
                        Year = year,
                        Activities = activities,
                        Id = item.ID
                    });
                }

                return spActivities;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing activities from SP failed. {e.Message}");
            }
        }

        public IEnumerable<SpActivity> CalculateNewSpActivities(IEnumerable<ActivityInfo> activities,
            IEnumerable<SpActivity> spActivities, SPWeb web)
        {
            try
            {
                _logger?.LogInformation("Calculating new activities");

                var newSpActivities = new List<SpActivity>();

                foreach (var item in activities)
                {
                    var spActivity = spActivities.FirstOrDefault(x =>
                        x.Year == item.Date.Year && x.Month == item.Date.Month && x.User.Email == item.UserEmail);
                    if (spActivity != null)
                    {
                        if (spActivity.Activities.Contains(item.Activity)) continue;

                        var newSpActivity = new SpActivity
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

                            newSpActivities.Add(new SpActivity
                            {
                                Month = item.Date.Month,
                                Year = item.Date.Year,
                                User = user,
                                Activities = new List<string>
                                {
                                    item.Activity
                                },
                                IsNew = true
                            });
                        }
                        catch
                        {
                            _logger?.LogWarning($"User {item.UserEmail} is not found in web user list");
                        }
                    }
                }

                return newSpActivities;
            }
            catch (Exception e)
            {
                throw new Exception($"Calculating new activities failed. {e.Message}");
            }
        }

        public void UpdateSpActivities(IEnumerable<SpActivity> newSpActivities, SPList spList)
        {
            try
            {
                _logger?.LogInformation("Updating SP activities list");

                foreach (var item in newSpActivities)
                {
                    if (item.IsNew)
                    {
                        AddNewItem(spList, item);

                        continue;
                    }

                    if (item.IsNewActivity) AddItemActivity(spList, item);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Updating SP activities list failed. {e.Message}");
            }
        }

        private void AddNewItem(SPList spList, SpActivity item)
        {
            var newItem = spList.Items.Add();

            newItem[Constants.Activity.User] = item.User;
            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;
            var newActivitiesValue = new SPFieldMultiChoiceValue();

            foreach (var itemActivity in item.Activities) newActivitiesValue.Add(itemActivity);

            newItem[Constants.Activity.Activities] = newActivitiesValue;

            newItem.Update();
        }

        private void AddItemActivity(SPList spList, SpActivity item)
        {
            var itemToUpdate = spList.Items.GetItemById(item.Id);
            var activityField = itemToUpdate.Fields.GetField(Constants.Activity.Activities);
            var activityFieldValue =
                activityField.GetFieldValue(itemToUpdate[Constants.Activity.Activities].ToString()) as
                    SPFieldMultiChoiceValue;

            for (var i = 0; i < activityFieldValue.Count; i++) item.Activities.Remove(activityFieldValue[i]);

            foreach (var newItemActivity in item.Activities) activityFieldValue.Add(newItemActivity);

            activityField.Update();
        }
    }
}
