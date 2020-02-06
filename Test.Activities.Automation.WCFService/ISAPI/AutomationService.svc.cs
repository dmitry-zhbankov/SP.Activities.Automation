using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Activation;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib;

using Test.Activities.Automation.WCFService.Models;

namespace Test.Activities.Automation.WCFService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AutomationService : IAutomationService
    {
        private ILogger _logger;

        public AutomationService()
        {
            try
            {
                _logger=new ULSLogger(GetType().FullName);
            }
            catch
            {
            }
        }
        
        public HttpStatusCode FillActivities(IEnumerable<ActivityInfo> activities)
        {
            var statusCode = HttpStatusCode.Accepted;

            try
            {
                _logger?.LogInformation("Request received");

                try
                {
                    using (var site = new SPSite(Constants.Host))
                    using (var web = site.OpenWeb(Constants.Web))
                    {
                        var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                        if (spList == null) throw new Exception("Getting SP list failed");

                        var spActivities = GetSpActivities(spList);

                        var newSpActivities = CalculateNewSpActivities(activities, spActivities, web);

                        web.AllowUnsafeUpdates = true;

                        UpdateSpActivities(newSpActivities, spList, ref statusCode);

                        _logger.LogInformation("Request has been treated successfully");
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.Message);
                    statusCode = HttpStatusCode.InternalServerError;
                }

                _logger?.LogInformation("Sending response");
            }
            catch
            {
                statusCode = HttpStatusCode.InternalServerError;
            }

            return statusCode;
        }

        private IEnumerable<SpActivity> GetSpActivities(SPList spList)
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

        private IEnumerable<SpActivity> CalculateNewSpActivities(IEnumerable<ActivityInfo> activities,
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
                            _logger?.LogWarning($"User {item.UserEmail} not found in SP list");
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

        private void UpdateSpActivities(IEnumerable<SpActivity> newSpActivities, SPList spList,
            ref HttpStatusCode statusCode)
        {
            try
            {
                _logger?.LogInformation("Updating SP activities list");
                
                foreach (var item in newSpActivities)
                {
                    if (item.IsNew)
                    {
                        AddNewItem(spList, item, ref statusCode);

                        continue;
                    }

                    if (item.IsNewActivity) AddItemActivity(spList, item, ref statusCode);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Updating SP activities list failed. {e.Message}");
            }
        }

        private void AddNewItem(SPList spList, SpActivity item, ref HttpStatusCode statusCode)
        {
            var newItem = spList.Items.Add();

            newItem[Constants.Activity.User] = item.User;
            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;
            var newActivitiesValue = new SPFieldMultiChoiceValue();

            foreach (var itemActivity in item.Activities) newActivitiesValue.Add(itemActivity);

            newItem[Constants.Activity.Activities] = newActivitiesValue;

            newItem.Update();

            statusCode = HttpStatusCode.Created;
        }

        private void AddItemActivity(SPList spList, SpActivity item, ref HttpStatusCode statusCode)
        {
            var itemToUpdate = spList.Items.GetItemById(item.Id);
            var activityField = itemToUpdate.Fields.GetField(Constants.Activity.Activities);
            var activityFieldValue =
                activityField.GetFieldValue(itemToUpdate[Constants.Activity.Activities].ToString()) as
                    SPFieldMultiChoiceValue;

            if (activityFieldValue == null)
            {
                statusCode = HttpStatusCode.InternalServerError;
                return;
            }

            for (var i = 0; i < activityFieldValue.Count; i++) item.Activities.Remove(activityFieldValue[i]);

            foreach (var newItemActivity in item.Activities) activityFieldValue.Add(newItemActivity);

            activityField.Update();

            statusCode = HttpStatusCode.Created;
        }
    }
}
