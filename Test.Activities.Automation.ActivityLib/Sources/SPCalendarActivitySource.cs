using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
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
                var strDate = SPUtility.CreateISO8601DateTimeFromSystemDateTime(date);
                var dateRangeQuery = new SPQuery
                {
                    Query =
                        "<Where>" +
                            "<And>" +
                                "<Geq>" +
                                    $"<FieldRef Name=\"{Constants.Calendar.EndTime}\"/>" +
                                        $"<Value Type=\"DateTime\">{strDate}</Value>" +
                                "</Geq>" +
                                "<Leq>" +
                                    $"<FieldRef Name=\"{Constants.Calendar.StartTime}\"/>" +
                                        $"<Value Type=\"DateTime\">{strDate}</Value>" +
                                "</Leq>" +
                            "</And>" +
                        "</Where>"
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

                    var mentor =
                        _spMembers.FirstOrDefault(x => x.MentorLookupId == lookupMentorId);
                    var rootMentor =
                        _spMembers.FirstOrDefault(x => x.RootMentorLookupId == lookupRootMentorId);

                    if (mentor != null)
                    {
                        activities.Add(new ActivityInfo
                        {
                            UserId = mentor.UserId,
                            Activity = Constants.Activity.ActivityType.Mentoring,
                            Date = yesterday,
                            Paths = paths
                        });
                    }

                    if (rootMentor != null)
                    {
                        activities.Add(new ActivityInfo
                        {
                            UserId = rootMentor.UserId,
                            Activity = Constants.Activity.ActivityType.RootMentoring,
                            Date = yesterday,
                            Paths = paths
                        });
                    }
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
