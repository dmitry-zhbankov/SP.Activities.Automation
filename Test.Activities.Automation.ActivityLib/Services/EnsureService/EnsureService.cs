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

        public IEnumerable<SPActivity> Ensure(IEnumerable<SPActivity> spActivities, IEnumerable<ActivityInfo> activities, IEnumerable<SPMember> members)
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
                    .Select(x => new SPActivity()
                    {
                        SPActivityId = x.Value.ActivityId,
                        Activities = new List<string>(x.Value.Activities),
                        Year = x.Key.Year,
                        Month = x.Key.Month,
                        SPMember = x.Value.SPMember,
                        Paths = new List<string>(x.Value.Paths)
                    }).ToList();

                return itemsToUpdate;
            }
            catch (Exception e)
            {
                throw new Exception($"Ensuring activities failed. {e.Message}");
            }
        }

        private void UpdateDictionary(IDictionary<ActivityKey, ActivityValue> dict, ActivityInfo activity, IEnumerable<SPMember> members)
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

                var set = new SortedSet<string>(activity.Paths);
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
                    SPMember = newMember,
                    IsModified = true
                };

                dict.Add(newKey, newValue);
            }
        }

        private IDictionary<ActivityKey, ActivityValue> InitDictionary(IEnumerable<SPActivity> spActivities)
        {
            var dict = new Dictionary<ActivityKey, ActivityValue>();

            foreach (var spActivity in spActivities)
            {
                var key = new ActivityKey()
                {
                    UserId = spActivity.SPMember.UserId,
                    Year = spActivity.Year,
                    Month = spActivity.Month
                };

                var value = new ActivityValue()
                {
                    ActivityId = spActivity.SPActivityId,
                    Activities = new SortedSet<string>(spActivity.Activities),
                    Paths = new SortedSet<string>(spActivity.Paths),
                    SPMember = spActivity.SPMember
                };

                dict.Add(key, value);
            }

            return dict;
        }
    }
}
