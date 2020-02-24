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
        private ILogger _logger;
        private IEnumerable<SpMember> _spMembers;
        private SPList _spListMentoringCalendar;

        public SPCalendarActivitySource(ILogger logger, IEnumerable<SpMember> spMembers, SPList spListMentoringCalendar) : base(logger)
        {
            _logger = logger;
            _spMembers = spMembers;
            _spListMentoringCalendar = spListMentoringCalendar;
        }

        public IEnumerable<SPListItem> GetCalendarItems(DateTime date)
        {
            try
            {
                var dateRangeQuery = new SPQuery
                {
                    //Query = $"<Where><And><Geq><FieldRef Name=\"{Constants.Calendar.EndTime}\"/><Value Type=\"DateTime\">{date:yyyy-MM-dd}</Value></Geq><Leq><FieldRef Name=\"{Constants.Calendar.StartTime}\"/><Value Type=\"DateTime\">{date:yyyy-MM-dd}</Value></Leq></And></Where>"
                };

                var events = _spListMentoringCalendar.GetItems(dateRangeQuery).Cast<SPListItem>().ToList();
                return events;
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
                var yesterday = DateTime.Now.AddDays(-1).Date;

                var events = GetCalendarItems(yesterday);

                var activities = new List<ActivityInfo>();

                foreach (var item in events)
                {
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    var lookupRootMentorId = SPHelper.GetItemLookupId(item, Constants.Calendar.RootMentor);
                    var lookupMentorId = SPHelper.GetItemLookupId(item, Constants.Calendar.Mentor);

                    //var rootMentor = SPHelper.GetLookUpUserValue(_spListRootMentors, item, Constants.Calendar.RootMentor,
                    //    Constants.Activity.Employee);

                    var member =
                        _spMembers.FirstOrDefault(x => x.UserId == lookupMentorId || x.UserId == lookupRootMentorId);

                    if (member == null) continue;

                    if (member.MentorLookupId != null)
                    {
                        activities.Add(new ActivityInfo()
                        {
                            UserId = member.UserId,
                            Activity = Constants.Activity.ActivityType.Mentoring,
                            Date = yesterday,
                            Paths = paths
                        });
                    }

                    if (member.RootMentorLookupId != null)
                    {
                        activities.Add(new ActivityInfo
                        {
                            UserId = member.UserId,
                            Activity = Constants.Activity.ActivityType.RootMentoring,
                            Date = yesterday,
                            Paths = paths
                        });
                    }

                    //var mentor = SPHelper.GetLookUpUserValue(_spListMentors, item, Constants.Calendar.Mentor,
                    //    Constants.Activity.Employee);

                    //activities.Add(new ActivityInfo
                    //{
                    //    UserId = mentor.User.ID,
                    //    Activity = Constants.Activity.ActivityType.Mentoring,
                    //    Date = yesterday,
                    //    Paths = paths
                    //});
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
