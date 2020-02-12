using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SyncActivityService
    {
        private protected class ActivityKey : IEqualityComparer<ActivityKey>
        {
            public int UserId { get; set; }

            public int Year { get; set; }

            public int Month { get; set; }

            public bool Equals(ActivityKey x, ActivityKey y)
            {
                return x.UserId == y.UserId && x.Year == y.Year && x.Month == y.Month;
            }

            public int GetHashCode(ActivityKey obj)
            {
                return obj.UserId ^ obj.Year ^ obj.Month;
            }
        }

        private protected class ActivityValue
        {
            public int ActivityId { get; set; }

            public SPUser Mentor { get; set; }

            public SPUser RootMentor { get; set; }

            public HashSet<string> Paths { get; set; }

            public HashSet<string> Activities { get; set; }

            public bool IsModified { get; set; }
        }

        private protected abstract class Member
        {
            public SPUser Mentor { get; set; }

            public SPUser RootMentor { get; set; }

            public IEnumerable<string> Paths { get; set; }
        }

        private ILogger _logger;

        public SyncActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public void Sync(IEnumerable<ActivityInfo> activities)
        {
            _logger?.LogInformation("Synchronizing activities");

            using (var site = new SPSite(Constants.Host))
            using (var web = site.OpenWeb(Constants.Web))
            {
                var spActivities = GetSpActivities(web);

                var members = GetSpMembers(web);

                var compareDict = Ensure(spActivities, activities, members);

                web.AllowUnsafeUpdates = true;

                UpdateSpActivities(compareDict, web);
            }
        }

        private IEnumerable<Member> GetSpMembers(SPWeb web)
        {
            List<Member> members = new List<Member>();

            return members;
        }

        private Dictionary<ActivityKey, ActivityValue> Ensure(IEnumerable<SpActivity> spActivities, IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
        {
            var dict = InitDictionary(spActivities);

            foreach (var activity in activities)
            {
                var key = new ActivityKey()
                {
                    UserId = activity.UserId,
                    Year = activity.Date.Year,
                    Month = activity.Date.Month
                };

                if (dict.ContainsKey(key))
                {
                    var value = dict[key];
                    if (!value.Activities.Contains(activity.Activity))
                    {
                        value.Activities.Add(activity.Activity);
                        dict[key].IsModified = true;
                    }

                    var set = new HashSet<string>(activity.Paths);
                    set.ExceptWith(value.Paths);

                    if (set.Count > 0)
                    {
                        foreach (var item in set)
                        {
                            value.Paths.Add(item);
                        }
                        dict[key].IsModified = true;
                    }
                }
                else
                {
                    var newMember = members.FirstOrDefault(x =>
                        x.Mentor?.ID == activity.UserId || x.RootMentor?.ID == activity.UserId);

                    if (newMember != null)
                    {
                        var newKey = new ActivityKey()
                        {
                            UserId = activity.UserId,
                            Year = activity.Date.Year,
                            Month = activity.Date.Month
                        };
                        var newValue = new ActivityValue()
                        {
                            Activities = new HashSet<string>()
                            {
                                activity.Activity
                            },
                            Paths = new HashSet<string>(activity.Paths),
                            Mentor = newMember.Mentor,
                            RootMentor = newMember.RootMentor,
                            IsModified = true
                        };

                        dict.Add(newKey, newValue);
                    }
                }
            }

            return dict;
        }

        Dictionary<ActivityKey, ActivityValue> InitDictionary(IEnumerable<SpActivity> spActivities)
        {
            var dict = new Dictionary<ActivityKey, ActivityValue>();

            foreach (var spActivity in spActivities)
            {
                int userId = 0;

                if (spActivity.RootMentor != null)
                {
                    userId = spActivity.RootMentor.ID;
                }
                else if (spActivity.Mentor != null)
                {
                    userId = spActivity.Mentor.ID;
                }

                var key = new ActivityKey()
                {
                    UserId = userId,
                    Year = spActivity.Year,
                    Month = spActivity.Month
                };

                var value = new ActivityValue()
                {
                    ActivityId = spActivity.Id,
                    Activities = new HashSet<string>(spActivity.Activities),
                    Paths = new HashSet<string>(spActivity.Paths),
                    Mentor = spActivity.Mentor,
                    RootMentor = spActivity.RootMentor
                };

                dict.Add(key, value);
            }

            return dict;
        }

        public IEnumerable<SpActivity> GetSpActivities(SPWeb web)
        {
            try
            {
                _logger?.LogInformation("Getting existing activities from SP");

                var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spList == null) throw new Exception("Getting SP list failed");

                var spActivities = new List<SpActivity>();

                foreach (var item in spList.GetItems().Cast<SPListItem>())
                {
                    var mentorField = item.Fields.GetField(Constants.Activity.Mentor);
                    var mentorFieldValue =
                        mentorField.GetFieldValue(item[Constants.Activity.Mentor].ToString()) as SPFieldUserValue;
                    var mentor = mentorFieldValue?.User;

                    var rootMentorField = item.Fields.GetField(Constants.Activity.RootMentor);
                    var rootMentorFieldValue =
                        rootMentorField.GetFieldValue(item[Constants.Activity.RootMentor].ToString()) as SPFieldUserValue;
                    var rootMentor = rootMentorFieldValue?.User;

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
                        RootMentor = rootMentor,
                        Mentor = mentor,
                        Month = month,
                        Year = year,
                        Activities = activities,
                        Id = item.ID,
                    });
                }

                return spActivities;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing activities from SP failed. {e.Message}");
            }
        }

        void UpdateSpActivities(Dictionary<ActivityKey, ActivityValue> dict, SPWeb web)
        {
            try
            {
                _logger?.LogInformation("Updating SP activities list");

                var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spList == null) throw new Exception("Getting SP list failed");

                var itemsToUpdate = dict.Where(x => x.Value.IsModified)
                    .Select(x => new SpActivity()
                    {
                        Id = x.Value.ActivityId,
                        Activities = new List<string>(x.Value.Activities),
                        Year = x.Key.Year,
                        Month = x.Key.Month,
                        Mentor = x.Value.Mentor,
                        RootMentor = x.Value.RootMentor,
                        Paths = new List<string>(x.Value.Paths)
                    });

                foreach (var item in itemsToUpdate)
                {
                    if (item.IsNew)
                    {
                        InsertActivity(spList, item);
                    }
                    else
                    {
                        UpdateActivity(spList, item);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Updating SP activities list failed. {e.Message}");
            }
        }

        private void InsertActivity(SPList spList, SpActivity item)
        {
            var newItem = spList.Items.Add();

            newItem[Constants.Activity.Mentor] = item.Mentor;
            newItem[Constants.Activity.RootMentor] = item.RootMentor;
            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;
            var newActivitiesValue = new SPFieldMultiChoiceValue();

            foreach (var itemActivity in item.Activities) newActivitiesValue.Add(itemActivity);

            newItem[Constants.Activity.Activities] = newActivitiesValue;

            newItem.Update();
        }

        private void UpdateActivity(SPList spList, SpActivity item)
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
