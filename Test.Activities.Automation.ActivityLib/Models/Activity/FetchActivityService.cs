using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class FetchActivityService
    {
        private ILogger _logger;

        public FetchActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<ActivityInfo> GetActivities()
        {
            var devActivities = GetDevActivities();

            var mentoringActivities = GetMentoringActivities();

            return devActivities.Concat(mentoringActivities).ToList();
        }
        
        public IEnumerable<ActivityInfo> GetDevActivities()
        {
            try
            {
                _logger?.LogInformation("Getting dev activities");

                var repoService=new RepositoryService();

                var repositories = repoService.GetConfigRepositories();

                repoService.GetRepoCommits(repositories);

                return CreateRepoActivities(repositories);
            }
            catch (Exception e)
            {
                _logger?.LogError($"Getting dev activities failed. {e.Message}");
                return new List<ActivityInfo>();
            }
        }

        public IEnumerable<ActivityInfo> CreateRepoActivities(IEnumerable<Repository> repositories)
        {
            var activities = new List<ActivityInfo>();
            foreach (var repo in repositories)
            foreach (var branch in repo.Branches)
                activities.AddRange(branch.Commits.Select(commit => new ActivityInfo
                    { Activity = repo.Activity, Date = DateTime.Parse(commit.Date), UserEmail = commit.AuthorEmail }));

            return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
        }

        public  IEnumerable<ActivityInfo> GetMentoringActivities()
        {
            try
            {
                _logger?.LogInformation("Getting mentoring activities");

                var activities = new List<ActivityInfo>();
                SPList mentoringCalendarList;
                SPList mentorList;
                SPList rootMentorList;

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    mentoringCalendarList = web.Lists[Constants.Lists.MentoringCalendar];
                    rootMentorList = web.Lists[Constants.Lists.RootMentors];
                    mentorList = web.Lists[Constants.Lists.Mentors];
                }

                var eventsCollection = mentoringCalendarList.GetItems();
                var yesterday = DateTime.Now.AddDays(-1).Date;
                var events = eventsCollection.Cast<SPListItem>()
                    .Where(x => x[Constants.Calendar.StartTime] as DateTime? <= yesterday)
                    .Where(x => x[Constants.Calendar.EndTime] as DateTime? >= yesterday);

                foreach (var item in events)
                {
                    var rootMentor = GetLookUpUser(rootMentorList, item, Constants.Calendar.RootMentor,
                        Constants.Activity.Employee);
                        
                    activities.Add(new ActivityInfo
                    {
                        UserEmail = rootMentor.Email,
                        Activity = Constants.Activity.ActivityType.RootMentoring,
                        Date = yesterday
                    });

                    var mentor = GetLookUpUser(mentorList, item, Constants.Calendar.Mentor,
                        Constants.Activity.Employee);

                    activities.Add(new ActivityInfo
                    {
                        UserEmail = mentor.Email,
                        Activity = Constants.Activity.ActivityType.Mentoring,
                        Date = yesterday
                    });
                }

                return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
            }
            catch (Exception e)
            {
                _logger.LogError($"Getting mentoring activities failed. {e.Message}");
                return new List<ActivityInfo>();
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
