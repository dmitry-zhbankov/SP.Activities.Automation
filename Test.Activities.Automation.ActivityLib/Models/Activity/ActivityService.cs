using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.Web.Hosting.Administration;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class ActivityService
    {
        private ILogger _logger;

        public ActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<SpActivity> GetActivities()
        {
            var devActivities = ActivityInfoToSPActivity(GetDevActivities());

            var mentoringActivities = GetMentoringActivities();

            return devActivities.Concat(mentoringActivities).ToList();
        }

        public IEnumerable<ActivityInfo> GetDevActivities()
        {
            try
            {
                _logger?.LogInformation("Getting dev activities");

                var repoService = new RepositoryService();

                var repositories = repoService.GetConfigRepositories();

                repoService.GetRepoCommits(repositories);

                return CreateDevActivities(repositories);
            }
            catch (Exception e)
            {
                _logger?.LogError($"Getting dev activities failed. {e.Message}");
                return new List<ActivityInfo>();
            }
        }

        public IEnumerable<ActivityInfo> CreateDevActivities(IEnumerable<Repository> repositories)
        {
            var activities = new List<ActivityInfo>();
            foreach (var repo in repositories)
                foreach (var branch in repo.Branches)
                    activities.AddRange(branch.Commits.Select(commit => new ActivityInfo
                    { Activity = repo.Activity, Date = DateTime.Parse(commit.Date), UserEmail = commit.AuthorEmail }));

            return activities; //.GroupBy(x => x.UserEmail).Select(x => x.First());
        }

        public IEnumerable<SpActivity> ActivityInfoToSPActivity(IEnumerable<ActivityInfo> activities)
        {
            var spActivities = new List<SpActivity>();

            var userActivities = activities.GroupBy(x => new { x.UserEmail, x.Date.Year, x.Date.Month });

            //using (var site = new SPSite(Constants.Host))
            //using (var web = site.OpenWeb(Constants.Web))
            //{
            //    foreach (var item in userActivities)
            //    {
            //        SPUser user = null;
            //        try
            //        {
            //            user = web.Users.GetByEmail(item.UserEmail);
            //        }
            //        catch (Exception e)
            //        {
            //        }

            //        if (user != null)
            //        {
            //            spActivities.Add(new SpActivity()
            //            {
            //                User = user,
            //            });
            //        }
            //    }
            //}

            //foreach (var activity in spActivities)
            //{
            //    activity.Activities = new List<string>(activities.Where(x => x.UserEmail == activity.User.Email)
            //        .Select(x => x.Activity));
            //}

            return spActivities;
        }

        public IEnumerable<SpActivity> GetMentoringActivities()
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
                    mentorList = web.Lists[Constants.Lists.Mentors];
                    rootMentorList = web.Lists[Constants.Lists.RootMentors];
                }

                var eventsCollection = mentoringCalendarList.GetItems();//SPQuery CAMLQuery
                var yesterday = DateTime.Now.AddDays(-1).Date;
                var events = eventsCollection.Cast<SPListItem>()
                    .Where(x => x[Constants.Calendar.StartTime] as DateTime? <= yesterday)
                    .Where(x => x[Constants.Calendar.EndTime] as DateTime? >= yesterday);

                foreach (var item in events)
                {
                    var mentorField = mentorList.Fields.GetField(Constants.Activity.Employee);
                    try
                    {
                        var mentorFieldLookupValue = new SPFieldLookupValue(item[Constants.Calendar.Mentor].ToString());//try get look lookupValue if mentor = null
                        var mentorFieldValue =
                            mentorField.GetFieldValue(mentorFieldLookupValue.LookupId) as SPFieldUserValue;
                        activities.Add(new ActivityInfo
                        {
                            UserEmail = mentorFieldValue?.User.Email,
                            Activity = Constants.Activity.ActivityType.Mentoring,
                            Date = yesterday
                        });
                    }
                    catch (NullReferenceException e)
                    {
                    }

                    var rootMentorField = rootMentorList.Fields.GetField(Constants.Activity.Employee);
                    try
                    {
                        var rootMentorFieldLookupValue = new SPFieldLookupValue(item[Constants.Calendar.RootMentor].ToString());
                        var rootMentorFieldValue =
                            rootMentorField.GetFieldValue(rootMentorFieldLookupValue.ToString()) as SPFieldUserValue;
                        activities.Add(new ActivityInfo
                        {
                            UserEmail = rootMentorFieldValue?.User.Email,
                            Activity = Constants.Activity.ActivityType.RootMentoring,
                            Date = yesterday
                        });
                    }
                    catch (NullReferenceException e)
                    {
                    }
                }

                return new List<SpActivity>();
                //return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
            }
            catch (Exception e)
            {
                _logger.LogError($"Getting mentoring activities failed. {e.Message}");
                return new List<SpActivity>();
            }
        }

        public void SynchronizeSpActivities(IEnumerable<SpActivity> activities)
        {
            using (var site = new SPSite(Constants.Host))
            using (var web = site.OpenWeb(Constants.Web))
            {
                var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spList == null) throw new Exception("Getting SP list failed");

                var spActivityService = new SpActivityService(_logger);

                web.AllowUnsafeUpdates = true;

                var spActivities = GetExistingSpActivities(spList);

                //var newSpActivities = CalculateNewSpActivities(activities, spActivities, web);

                var newSpActivities = new List<SpActivity>();

                UpdateSpActivities(newSpActivities, spList);

                _logger.LogInformation("Request has been treated successfully");
            }
        }

        public IEnumerable<SpActivity> GetExistingSpActivities(SPList spList)
        {
            try
            {
                _logger?.LogInformation("Getting existing activities from SP");

                var spActivities = new List<SpActivity>();

                foreach (var item in spList.GetItems().Cast<SPListItem>())//new SPQuery() current month, year
                {
                    var userField = item.Fields.GetField(Constants.Activity.User);
                    var userFieldValue =
                        userField.GetFieldValue(item[Constants.Activity.User].ToString()) as SPFieldUserValue;
                    var user = userFieldValue.User;

                    var month = Convert.ToInt32(item[Constants.Activity.Month]);
                    var year = Convert.ToInt32(item[Constants.Activity.Year]);

                    var activityField = item.Fields.GetField(Constants.Activity.Activities);
                    var activityFieldValue =
                        activityField.GetFieldValue(item[Constants.Activity.Activities].ToString()) as
                            SPFieldMultiChoiceValue;

                    var activities = new List<string>();
                    for (var i = 0; i < activityFieldValue.Count; i++) activities.Add(activityFieldValue[i]);

                    spActivities.Add(new SpActivity
                    {
                        User = user,
                        Month = month,
                        Year = year,
                        Activities = activities,
                        Id = item.ID
                    });
                }

                return spActivities;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing activities from SP failed. {e.Message}");
            }
        }

        //public IEnumerable<SpActivity> CalculateNewSpActivities(IEnumerable<ActivityInfo> activities,
        //    IEnumerable<SpActivity> spActivities, SPWeb web)
        //{
        //    try
        //    {
        //        _logger?.LogInformation("Calculating new activities");

        //        var newSpActivities = new List<SpActivity>();

        //        foreach (var item in activities)
        //        {
        //            var spActivity = spActivities.FirstOrDefault(x =>
        //                x.Year == item.Date.Year && x.Month == item.Date.Month && x.User.Email == item.UserEmail);
        //            if (spActivity != null)
        //            {
        //                if (spActivity.Activities.Contains(item.Activity)) continue;

        //                var newSpActivity = new SpActivity
        //                {
        //                    Id = spActivity.Id,
        //                    Month = spActivity.Month,
        //                    User = spActivity.User,
        //                    Year = spActivity.Year,
        //                    Activities = new List<string>(spActivity.Activities),
        //                    IsNewActivity = true
        //                };

        //                newSpActivity.Activities.Add(item.Activity);
        //            }
        //            else
        //            {
        //                var act = newSpActivities.FirstOrDefault(x => x.User.Email == item.UserEmail);
        //                if (act != null)
        //                {
        //                    act.Activities.Add(item.Activity);
        //                    continue;
        //                }

        //                try
        //                {
        //                    var user = web.Users.GetByEmail(item.UserEmail);

        //                    newSpActivities.Add(new SpActivity
        //                    {
        //                        Month = item.Date.Month,
        //                        Year = item.Date.Year,
        //                        User = user,
        //                        Activities = new List<string>
        //                        {
        //                            item.Activity
        //                        }
        //                    });
        //                }
        //                catch
        //                {
        //                    _logger?.LogWarning($"User {item.UserEmail} is not found in web user list");
        //                }
        //            }
        //        }

        //        return newSpActivities;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception($"Calculating new activities failed. {e.Message}");
        //    }
        //}

        public void UpdateSpActivities(IEnumerable<SpActivity> newSpActivities, SPList spList)
        {
            try
            {
                _logger?.LogInformation("Updating SP activities list");

                foreach (var item in newSpActivities)
                {
                    if (item.IsNew)
                    {
                        AddNewSPItem(spList, item);

                        continue;
                    }

                    if (item.IsNewActivity) AddNewActivityToSPItem(spList, item);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Updating SP activities list failed. {e.Message}");
            }
        }

        private void AddNewSPItem(SPList spList, SpActivity item)
        {
            var newItem = spList.Items.Add();

            newItem[Constants.Activity.User] = item.User;
            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;
            var newActivitiesValue = new SPFieldMultiChoiceValue();

            foreach (var itemActivity in item.Activities) newActivitiesValue.Add(itemActivity);

            newItem[Constants.Activity.Activities] = newActivitiesValue;

            newItem.Update();
        }

        private void AddNewActivityToSPItem(SPList spList, SpActivity item)
        {
            var itemToUpdate = spList.Items.GetItemById(item.Id);
            var activityField = itemToUpdate.Fields.GetField(Constants.Activity.Activities);
            var activityFieldValue =
                activityField.GetFieldValue(itemToUpdate[Constants.Activity.Activities].ToString()) as
                    SPFieldMultiChoiceValue;

            for (var i = 0; i < activityFieldValue.Count; i++) item.Activities.Remove(activityFieldValue[i]);

            foreach (var newItemActivity in item.Activities) activityFieldValue.Add(newItemActivity);

            activityField.Update();
        }
    }
}