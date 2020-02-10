using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SPCalendarActivitySource : ActivitySource
    {
        private IEnumerable<SPListItem> _events;
        private SPList _mentorList;
        private SPList _rootMentorList;
        private DateTime _yesterday;

        public SPCalendarActivitySource(ILogger logger) : base(logger)
        {
        }

        public override void Configure()
        {
            try
            {
                SPList mentoringCalendarList;

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    mentoringCalendarList = web.Lists[Constants.Lists.MentoringCalendar];
                    _rootMentorList = web.Lists[Constants.Lists.RootMentors];
                    _mentorList = web.Lists[Constants.Lists.Mentors];
                }

                var eventsCollection = mentoringCalendarList.GetItems();
                _yesterday = DateTime.Now.AddDays(-1).Date;
                _events = eventsCollection.Cast<SPListItem>()
                    .Where(x => x[Constants.Calendar.StartTime] as DateTime? <= _yesterday)
                    .Where(x => x[Constants.Calendar.EndTime] as DateTime? >= _yesterday);
            }
            catch (Exception e)
            {
                throw new Exception($"Getting mentoring calendar configuration failed. {e.Message}");
            }
        }

        public override IEnumerable<ActivityInfo> FetchActivity()
        {
            _logger?.LogInformation("Fetching mentoring activities");
            
            try
            {
                var activities = new List<ActivityInfo>();

                foreach (var item in _events)
                {
                    var rootMentor = GetLookUpUser(_rootMentorList, item, Constants.Calendar.RootMentor,
                        Constants.Activity.Employee);

                    activities.Add(new ActivityInfo
                    {
                        UserEmail = rootMentor.Email,
                        Activity = Constants.Activity.ActivityType.RootMentoring,
                        Date = _yesterday
                    });

                    var mentor = GetLookUpUser(_mentorList, item, Constants.Calendar.Mentor,
                        Constants.Activity.Employee);

                    activities.Add(new ActivityInfo
                    {
                        UserEmail = mentor.Email,
                        Activity = Constants.Activity.ActivityType.Mentoring,
                        Date = _yesterday
                    });
                }

                return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
            }
            catch (Exception e)
            {
                _logger.LogError($"Fetching mentoring activities failed. {e.Message}");
                return null;
            }
        }

        SPUser GetLookUpUser(SPList originList, SPListItem item, string lookUpField, string originField)
        {
            var userLookUpField = item.Fields.GetField(lookUpField);
            var userFieldLookUpValue =
                userLookUpField.GetFieldValue(item[lookUpField].ToString()) as SPFieldLookupValue;
            var rootMentorField = originList.Fields.GetField(originField);
            var rootMentorItem = originList.GetItemById(userFieldLookUpValue.LookupId);
            var rootMentorFieldValue =
                rootMentorField.GetFieldValue(rootMentorItem[originField].ToString()) as SPFieldUserValue;

            return rootMentorFieldValue.User;
        }
    }
}
