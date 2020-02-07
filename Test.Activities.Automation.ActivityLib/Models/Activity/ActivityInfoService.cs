using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class ActivityInfoService
    {
        private ILogger _logger;

        public ActivityInfoService(ILogger logger)
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
                SPList mentoringList;

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    mentoringList = web.Lists[Constants.Lists.MentoringCalendar];
                }

                var eventsCollection = mentoringList.GetItems();
                var yesterday = DateTime.Now.AddDays(-1).Date;
                var events = eventsCollection.Cast<SPListItem>()
                    .Where(x => x[Constants.Calendar.StartTime] as DateTime? <= yesterday)
                    .Where(x => x[Constants.Calendar.EndTime] as DateTime? >= yesterday);

                foreach (var item in events)
                {
                    var userField = item.Fields.GetField(Constants.Calendar.Employee);
                    var userFieldValue =
                        userField.GetFieldValue(item[Constants.Calendar.Employee].ToString()) as SPFieldUserValue;
                    activities.Add(new ActivityInfo
                    {
                        UserEmail = userFieldValue?.User.Email,
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
    }
}
