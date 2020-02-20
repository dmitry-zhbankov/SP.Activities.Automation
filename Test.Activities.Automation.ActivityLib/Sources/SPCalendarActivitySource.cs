using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Helpers;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Sources
{
    public class SPCalendarActivitySource : ActivitySource
    {
        private IEnumerable<SPListItem> _events;
        private SPList _mentorList;
        private SPList _rootMentorList;
        private DateTime _yesterday;
        private SPWeb _web;

        public SPCalendarActivitySource(ILogger logger, SPWeb web) : base(logger)
        {
            _web = web;
        }

        public override void Configure()
        {
            try
            {
                var mentoringCalendarList = _web.Lists[Constants.Lists.MentoringCalendar];
                _rootMentorList = _web.Lists[Constants.Lists.RootMentors];
                _mentorList = _web.Lists[Constants.Lists.Mentors];

                _yesterday = DateTime.Now.AddDays(-1).Date;
                var dateRangeQuery = new SPQuery
                {
                    Query =
                        $"<Where><And><Geq><FieldRef Name=\"{Constants.Calendar.EndTime}\"/><Value Type=\"DateTime\">{_yesterday:yyyy-MM-dd}</Value></Geq><Leq><FieldRef Name=\"{Constants.Calendar.StartTime}\"/><Value Type=\"DateTime\">{_yesterday:yyyy-MM-dd}</Value></Leq></And></Where>"
                };

                _events = mentoringCalendarList.GetItems(dateRangeQuery).Cast<SPListItem>();
            }
            catch (Exception e)
            {
                throw new Exception($"Getting mentoring calendar configuration failed. {e}");
            }
        }

        public override IEnumerable<ActivityInfo> FetchActivities()
        {
            _logger?.LogInformation("Fetching mentoring activities");

            try
            {
                var activities = new List<ActivityInfo>();

                foreach (var item in _events)
                {
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    var rootMentor = SPHelper.GetLookUpUserValue(_rootMentorList, item, Constants.Calendar.RootMentor,
                        Constants.Activity.Employee);

                    activities.Add(new ActivityInfo
                    {
                        UserId = rootMentor.User.ID,
                        Activity = Constants.Activity.ActivityType.RootMentoring,
                        Date = _yesterday,
                        Paths = paths
                    });

                    var mentor = SPHelper.GetLookUpUserValue(_mentorList, item, Constants.Calendar.Mentor,
                        Constants.Activity.Employee);

                    activities.Add(new ActivityInfo
                    {
                        UserId = mentor.User.ID,
                        Activity = Constants.Activity.ActivityType.Mentoring,
                        Date = _yesterday,
                        Paths = paths
                    });
                }

                return activities;
            }
            catch (Exception e)
            {
                _logger?.LogError($"Fetching mentoring activities failed. {e}");
                return null;
            }
        }
    }
}
