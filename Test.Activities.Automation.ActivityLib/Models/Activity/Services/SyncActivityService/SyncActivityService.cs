using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models.Helpers;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.SyncActivityService
{
    public partial class SyncActivityService
    {
        private ILogger _logger;

        public SyncActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public void SyncActivities(IEnumerable<ActivityInfo> activities)
        {
            try
            {
                _logger?.LogInformation("Synchronizing activities");

                var now = DateTime.Now;
                var minDate = new DateTime(now.Year, now.AddMonths(-1).Month, 1);
                var maxDate = now.Date;

                CheckActivityDate(ref activities, minDate, maxDate);

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    var spActivities = GetSpActivities(web, minDate, maxDate);

                    var members = GetSpMembers(web);

                    CheckActivityUser(ref activities, members);

                    CheckActivityPaths(ref activities, members);

                    var compareDict = Ensure(spActivities, activities, members);

                    web.AllowUnsafeUpdates = true;

                    UpdateSpActivities(compareDict, web);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Synchronizing activities failed. {e.Message}");
            }
        }

        private void CheckActivityPaths(ref IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
        {
            foreach (var activity in activities)
            {
                if (activity.Paths == null || !activity.Paths.Any())
                {
                    var member = members.FirstOrDefault(x =>
                        x.MentorId == activity.UserId || x.RootMentorId == activity.UserId);

                    if (member != null)
                    {
                        activity.Paths = new List<string>(member.Paths);
                    }
                }
            }
        }

        private void CheckActivityDate(ref IEnumerable<ActivityInfo> activities, DateTime minDate, DateTime maxDate)
        {
            var checkedActivities = new List<ActivityInfo>();

            foreach (var activity in activities)
            {
                if (activity.Date >= minDate && activity.Date <= maxDate)
                {
                    checkedActivities.Add(activity);
                }
                else
                {
                    _logger.LogWarning($"Inconsistent activity date. Activity={APIHelper.JsonSerialize(activity)}");
                }
            }

            activities = checkedActivities;
        }

        private IEnumerable<Member> GetSpMembers(SPWeb web)
        {
            try
            {
                _logger?.LogInformation("Getting existing members from SP");

                var spMentorsList = web.Lists.TryGetList(Constants.Lists.Mentors);
                if (spMentorsList == null) throw new Exception("Getting SP mentor list failed");

                var spRootMentorsList = web.Lists.TryGetList(Constants.Lists.RootMentors);
                if (spRootMentorsList == null) throw new Exception("Getting SP root mentor list failed");

                var members = new List<Member>();

                foreach (var item in spMentorsList.GetItems().Cast<SPListItem>())
                {
                    var mentor = SPHelper.GetUserValue(item, Constants.Activity.Employee);

                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    members.Add(new Member
                    {
                        MentorId = mentor.User.ID,
                        MentorLookupId = item.ID,
                        MentorEmail = mentor.User.Email,
                        Paths = new List<string>(paths)
                    });
                }

                foreach (var item in spRootMentorsList.GetItems().Cast<SPListItem>())
                {
                    var rootMentor = SPHelper.GetUserValue(item, Constants.Activity.Employee);

                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    var member = members.FirstOrDefault(x => x.MentorId == rootMentor.User.ID);

                    if (member != null)
                    {
                        member.RootMentorId = rootMentor.User.ID;
                        member.RootMentorEmail = rootMentor.User.Email;
                        member.RootMentorLookupId = item.ID;
                    }
                    else
                    {
                        members.Add(new Member
                        {
                            RootMentorId = rootMentor.User.ID,
                            RootMentorLookupId = item.ID,
                            RootMentorEmail = rootMentor.User.Email,
                            Paths = new List<string>(paths)
                        });
                    }
                }

                return members;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing members from SP failed. {e.Message}");
            }
        }

        private Dictionary<ActivityKey, ActivityValue> Ensure(IEnumerable<SpActivity> spActivities, IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
        {
            try
            {
                _logger?.LogInformation("Ensuring activities");

                var dict = InitDictionary(spActivities);

                foreach (var activity in activities)
                {
                    UpdateDictionary(dict, activity, members);
                }

                return dict;
            }
            catch (Exception e)
            {
                throw new Exception($"Ensuring activities failed. {e.Message}");
            }
        }

        void CheckActivityUser(ref IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
        {
            var checkedActivities = new List<ActivityInfo>();

            foreach (var activity in activities)
            {
                if (activity.UserId != null)
                {
                    if (members.Any(x => x.MentorId == activity.UserId || x.RootMentorId == activity.UserId))
                    {
                        checkedActivities.Add(activity);
                        continue;
                    }
                }

                if (activity.UserEmail == null) continue;

                var id = GetUserIdByEmail(activity.UserEmail, members);
                if (id == null)
                {
                    continue;
                }

                activity.UserId = id;
                checkedActivities.Add(activity);
            }

            activities = checkedActivities;
        }

        private int? GetUserIdByEmail(string email, IEnumerable<Member> members)
        {
            var user = members.FirstOrDefault(x =>
                x.MentorEmail == email || x.RootMentorEmail == email);

            if (user == null) return null;

            var id = user.MentorId ?? user.RootMentorId;
            return id;
        }

        private void UpdateDictionary(Dictionary<ActivityKey, ActivityValue> dict, ActivityInfo activity, IEnumerable<Member> members)
        {
            var key = new ActivityKey()
            {
                UserId = (int)activity.UserId,
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
                    x.MentorId == activity.UserId || x.RootMentorId == activity.UserId);

                if (newMember != null)
                {
                    var newKey = key;

                    var newValue = new ActivityValue()
                    {
                        Activities = new HashSet<string>()
                            {
                                activity.Activity
                            },
                        Paths = new HashSet<string>(activity.Paths),
                        MentorId = newMember.MentorId,
                        MentorLookupId = newMember.MentorLookupId,
                        RootMentorId = newMember.RootMentorId,
                        RootMentorLookupId = newMember.RootMentorLookupId,
                        IsModified = true
                    };

                    dict.Add(newKey, newValue);
                }
            }
        }

        Dictionary<ActivityKey, ActivityValue> InitDictionary(IEnumerable<SpActivity> spActivities)
        {
            var dict = new Dictionary<ActivityKey, ActivityValue>();

            foreach (var spActivity in spActivities)
            {
                int userId = 0;

                if (spActivity.RootMentorId != null)
                {
                    userId = (int)spActivity.RootMentorId;
                }
                else if (spActivity.MentorId != null)
                {
                    userId = (int)spActivity.MentorId;
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
                    MentorId = spActivity.MentorId,
                    MentorLookupId = spActivity.MentorLookupId,
                    RootMentorId = spActivity.RootMentorId,
                    RootMentorLookupId = spActivity.RootMentorLookupId
                };

                dict.Add(key, value);
            }

            return dict;
        }

        public IEnumerable<SpActivity> GetSpActivities(SPWeb web, DateTime minDate, DateTime maxDate)
        {
            try
            {
                _logger?.LogInformation("Getting existing activities from SP");

                var spListActivities = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spListActivities == null) throw new Exception("Getting SP activity list failed");

                var spListMentors = web.Lists.TryGetList(Constants.Lists.Mentors);
                if (spListMentors == null) throw new Exception("Getting SP mentors list failed");

                var spListRootMentors = web.Lists.TryGetList(Constants.Lists.RootMentors);
                if (spListRootMentors == null) throw new Exception("Getting SP root mentor list failed");

                var spActivities = new List<SpActivity>();

                var dateRangeQuery = new SPQuery
                {
                    Query =
                        $"<Where><And><Geq><FieldRef Name=\"Date\"/><Value Type=\"DateTime\">{minDate:yyyy-MM-dd}</Value></Geq><Leq><FieldRef Name=\"Date\"/><Value Type=\"DateTime\">{maxDate:yyyy-MM-dd}</Value></Leq></And></Where>"
                };

                foreach (var item in spListActivities.GetItems(dateRangeQuery).Cast<SPListItem>())
                {
                    var mentor = SPHelper.GetLookUpUserValue(spListMentors, item, Constants.Activity.Mentor, Constants.Activity.Employee);
                    var mentorValue = SPHelper.GetLookUpItemId(spListMentors, item, Constants.Activity.Mentor,
                        Constants.Activity.Employee);

                    var rootMentor = SPHelper.GetLookUpUserValue(spListRootMentors, item, Constants.Activity.RootMentor, Constants.Activity.Employee);
                    var rootMentorValue = SPHelper.GetLookUpItemId(spListRootMentors, item, Constants.Activity.RootMentor, Constants.Activity.Employee);

                    var month = SPHelper.GetIntValue(item, Constants.Activity.Month);
                    var year = SPHelper.GetIntValue(item, Constants.Activity.Year);

                    var activities = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Activities);
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    spActivities.Add(new SpActivity
                    {
                        RootMentorId = rootMentor?.User.ID,
                        RootMentorLookupId = rootMentorValue,
                        MentorId = mentor?.User.ID,
                        MentorLookupId = mentorValue,
                        Month = month,
                        Year = year,
                        Activities = new List<string>(activities),
                        Paths = new List<string>(paths),
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
                        MentorId = x.Value.MentorId,
                        MentorLookupId = x.Value.MentorLookupId,
                        RootMentorId = x.Value.RootMentorId,
                        RootMentorLookupId = x.Value.RootMentorLookupId,
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

            if (item.MentorId != null)
            {
                newItem[Constants.Activity.Mentor] = item.MentorLookupId;
            }

            if (item.RootMentorId != null)
            {
                newItem[Constants.Activity.RootMentor] = item.RootMentorLookupId;
            }

            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;

            SPHelper.SetMultiChoiceValue(newItem, Constants.Activity.Activities, item.Activities);
            SPHelper.SetMultiChoiceValue(newItem, Constants.Activity.Paths, item.Paths);

            newItem.Update();
        }

        private void UpdateActivity(SPList spList, SpActivity item)
        {
            var itemToUpdate = spList.Items.GetItemById(item.Id);

            SPHelper.SetMultiChoiceValue(itemToUpdate, Constants.Activity.Activities, item.Activities);
            SPHelper.SetMultiChoiceValue(itemToUpdate, Constants.Activity.Paths, item.Paths);

            itemToUpdate.Update();
        }
    }
}
