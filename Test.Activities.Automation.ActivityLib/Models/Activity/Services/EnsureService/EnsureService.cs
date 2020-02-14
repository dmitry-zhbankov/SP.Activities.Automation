using System;
using System.Collections.Generic;
using System.Linq;
using Test.Activities.Automation.ActivityLib.Models.Activity.Classes;
using Test.Activities.Automation.ActivityLib.Models.Helpers;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services
{
    public partial class EnsureService
    {
        private ILogger _logger;

        public EnsureService(ILogger logger)
        {
            _logger = logger;
        }

        public void CheckActivityDate(ref IEnumerable<ActivityInfo> activities, DateTime minDate, DateTime maxDate)
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
                    _logger?.LogWarning($"Inconsistent activity date. Activity={APIHelper.JsonSerialize(activity)}");
                }
            }

            activities = checkedActivities;
        }

        public void CheckActivityPaths(ref IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
        {
            foreach (var activity in activities)
            {
                if (activity.Paths != null && activity.Paths.Any()) continue;

                var member = members.FirstOrDefault(x =>
                    x.MentorId == activity.UserId || x.RootMentorId == activity.UserId);

                if (member == null) continue;

                activity.Paths = new List<string>(member.Paths);
                _logger?.LogWarning($"Activity paths are empty. Getting from member's paths. Activity={APIHelper.JsonSerialize(activity)}");
            }
        }

        public void CheckActivityUser(ref IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
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

                    _logger?.LogWarning($"User with UserId={activity.UserId} is not found. Activity={APIHelper.JsonSerialize(activity)}");
                }

                if (activity.UserEmail == null)
                {
                    _logger?.LogWarning($"UserId and email are empty. Activity={APIHelper.JsonSerialize(activity)}");
                    continue;
                }

                var id = GetUserIdByEmail(activity.UserEmail, members);
                if (id == null)
                {
                    _logger?.LogWarning($"User with email={activity.UserEmail} is not found. Activity={APIHelper.JsonSerialize(activity)}");
                    continue;
                }

                activity.UserId = id;
                checkedActivities.Add(activity);
            }

            activities = checkedActivities;
        }

        public IEnumerable<SpActivity> Ensure(IEnumerable<SpActivity> spActivities, IEnumerable<ActivityInfo> activities, IEnumerable<Member> members)
        {
            try
            {
                _logger?.LogInformation("Ensuring activities");

                var dict = InitDictionary(spActivities);

                foreach (var activity in activities)
                {
                    UpdateDictionary(dict, activity, members);
                }

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

                return itemsToUpdate;
            }
            catch (Exception e)
            {
                throw new Exception($"Ensuring activities failed. {e.Message}");
            }
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

                if (set.Count == 0) return;

                foreach (var item in set)
                {
                    value.Paths.Add(item);
                }
                dict[key].IsModified = true;
            }
            else
            {
                var newMember = members.FirstOrDefault(x =>
                    x.MentorId == activity.UserId || x.RootMentorId == activity.UserId);

                if (newMember == null) return;
                
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

        Dictionary<ActivityKey, ActivityValue> InitDictionary(IEnumerable<SpActivity> spActivities)
        {
            var dict = new Dictionary<ActivityKey, ActivityValue>();

            foreach (var spActivity in spActivities)
            {
                var userId = 0;

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
    }
}
