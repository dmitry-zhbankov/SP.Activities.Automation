using System;
using System.Collections.Generic;
using System.Linq;
using Test.Activities.Automation.ActivityLib.Models;

namespace Test.Activities.Automation.ActivityLib.Services
{
    public partial class EnsureService
    {
        private ILogger _logger;

        public EnsureService(ILogger logger)
        {
            _logger = logger;
        }

        //public void CheckActivityDate(ref IEnumerable<ActivityInfo> activities, DateTime minDate, DateTime maxDate)
        //{
        //    var checkedActivities = new List<ActivityInfo>();

        //    foreach (var activity in activities)
        //    {
        //        if (activity.Date >= minDate && activity.Date <= maxDate)
        //        {
        //            checkedActivities.Add(activity);
        //        }
        //        else
        //        {
        //            _logger?.LogWarning($"Inconsistent activity date. Activity={APIHelper.JsonSerialize(activity)}");
        //        }
        //    }

        //    activities = checkedActivities;
        //}

        //public void CheckActivityPaths(ref IEnumerable<ActivityInfo> activities, IEnumerable<SpMember> spMembers)
        //{
        //    foreach (var activity in activities)
        //    {
        //        if (activity.Paths != null && activity.Paths.Any()) continue;

        //        var member = spMembers.FirstOrDefault(x => x.UserId == activity.UserId );

        //        if (member == null) continue;

        //        activity.Paths = new List<string>(member.Paths);
        //        _logger?.LogWarning($"Activity paths are empty. Getting from member's paths. Activity={APIHelper.JsonSerialize(activity)}");
        //    }
        //}

        //public void CheckActivityUser(ref IEnumerable<ActivityInfo> activities, IEnumerable<SpMember> spMembers)
        //{
        //    var checkedActivities = new List<ActivityInfo>();

        //    foreach (var activity in activities)
        //    {
        //        if (activity.UserId != null)
        //        {
        //            if (spMembers.Any(x => x.UserId == activity.UserId))
        //            {
        //                checkedActivities.Add(activity);
        //                continue;
        //            }

        //            _logger?.LogWarning($"User with UserId={activity.UserId} is not found. Activity={APIHelper.JsonSerialize(activity)}");
        //        }

        //        if (activity.UserEmail == null)
        //        {
        //            _logger?.LogWarning($"UserId and email are empty. Activity={APIHelper.JsonSerialize(activity)}");
        //            continue;
        //        }

        //        var id = GetUserIdByEmail(activity.UserEmail, spMembers);
        //        if (id == null)
        //        {
        //            _logger?.LogWarning($"User with email={activity.UserEmail} is not found. Activity={APIHelper.JsonSerialize(activity)}");
        //            continue;
        //        }

        //        activity.UserId = id;
        //        checkedActivities.Add(activity);
        //    }

        //    activities = checkedActivities;
        //}

        //public void Save(IEnumerable<ActivityInfo> activities)
        //{
        //    //ensureService.CheckActivityDate(ref activities, minDate, maxDate);

        //    //ensureService.CheckActivityUser(ref activities, spMembers);

        //    //ensureService.CheckActivityPaths(ref activities, spMembers);
        //}

        public IEnumerable<SpActivity> Ensure(IEnumerable<SpActivity> spActivities, IEnumerable<ActivityInfo> activities, IEnumerable<SpMember> members)
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
                        SpActivityId = x.Value.ActivityId,
                        Activities = new List<string>(x.Value.Activities),
                        Year = x.Key.Year,
                        Month = x.Key.Month,
                        SpMember = x.Value.SpMember,
                        Paths = new List<string>(x.Value.Paths)
                    }).ToList();

                return itemsToUpdate;
            }
            catch (Exception e)
            {
                throw new Exception($"Ensuring activities failed. {e.Message}");
            }
        }

        private void UpdateDictionary(IDictionary<ActivityKey, ActivityValue> dict, ActivityInfo activity, IEnumerable<SpMember> members)
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
                    x.UserId == activity.UserId);

                if (newMember == null) return;

                var newKey = key;

                var newValue = new ActivityValue()
                {
                    Activities = new SortedSet<string>()
                    {
                        activity.Activity
                    },
                    Paths = new SortedSet<string>(activity.Paths),
                    SpMember = newMember,
                    IsModified = true
                };

                dict.Add(newKey, newValue);
            }
        }

        private IDictionary<ActivityKey, ActivityValue> InitDictionary(IEnumerable<SpActivity> spActivities)
        {
            var dict = new Dictionary<ActivityKey, ActivityValue>();

            foreach (var spActivity in spActivities)
            {
                var key = new ActivityKey()
                {
                    UserId = spActivity.SpMember.UserId,
                    Year = spActivity.Year,
                    Month = spActivity.Month
                };

                var value = new ActivityValue()
                {
                    ActivityId = spActivity.SpActivityId,
                    Activities = new SortedSet<string>(spActivity.Activities),
                    Paths = new SortedSet<string>(spActivity.Paths),
                    SpMember = spActivity.SpMember
                };

                dict.Add(key, value);
            }

            return dict;
        }
    }
}
