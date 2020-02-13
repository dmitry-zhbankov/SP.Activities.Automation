using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models.Activity.Classes;
using Test.Activities.Automation.ActivityLib.Models.Helpers;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.SyncActivityService
{
    public class SyncActivityService
    {
        private ILogger _logger;

        public SyncActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public void SyncActivities(IEnumerable<ActivityInfo> activities)
        {
            try
            {
                _logger?.LogInformation("Synchronizing activities");

                if (activities == null)
                {
                    throw new Exception("Activities are null");
                }

                var now = DateTime.Now;
                var minDate = new DateTime(now.Year, now.AddMonths(-1).Month, 1);
                var maxDate = now.Date;

                var ensureService = new EnsureService(_logger);

                ensureService.CheckActivityDate(ref activities, minDate, maxDate);

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    var spActivities = GetSpActivities(web, minDate, maxDate);

                    var members = GetSpMembers(web);

                    ensureService.CheckActivityUser(ref activities, members);

                    ensureService.CheckActivityPaths(ref activities, members);

                    var itemsToUpdate = ensureService.Ensure(spActivities, activities, members);

                    web.AllowUnsafeUpdates = true;

                    UpdateSpActivities(itemsToUpdate, web);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Synchronizing activities failed. {e.Message}");
            }
        }

        private IEnumerable<Member> GetSpMembers(SPWeb web)
        {
            try
            {
                _logger?.LogInformation("Getting existing members from SP");

                var spMentorsList = web.Lists.TryGetList(Constants.Lists.Mentors);
                if (spMentorsList == null) throw new Exception("Getting SP mentor list failed");

                var spRootMentorsList = web.Lists.TryGetList(Constants.Lists.RootMentors);
                if (spRootMentorsList == null) throw new Exception("Getting SP root mentor list failed");

                var members = new List<Member>();

                foreach (var item in spMentorsList.GetItems().Cast<SPListItem>())
                {
                    var mentor = SPHelper.GetUserValue(item, Constants.Activity.Employee);

                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    members.Add(new Member
                    {
                        MentorId = mentor.User.ID,
                        MentorLookupId = item.ID,
                        MentorEmail = mentor.User.Email,
                        Paths = new List<string>(paths)
                    });
                }

                foreach (var item in spRootMentorsList.GetItems().Cast<SPListItem>())
                {
                    var rootMentor = SPHelper.GetUserValue(item, Constants.Activity.Employee);

                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    var member = members.FirstOrDefault(x => x.MentorId == rootMentor.User.ID);

                    if (member != null)
                    {
                        member.RootMentorId = rootMentor.User.ID;
                        member.RootMentorEmail = rootMentor.User.Email;
                        member.RootMentorLookupId = item.ID;
                    }
                    else
                    {
                        members.Add(new Member
                        {
                            RootMentorId = rootMentor.User.ID,
                            RootMentorLookupId = item.ID,
                            RootMentorEmail = rootMentor.User.Email,
                            Paths = new List<string>(paths)
                        });
                    }
                }

                return members;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing members from SP failed. {e.Message}");
            }
        }

        public IEnumerable<SpActivity> GetSpActivities(SPWeb web, DateTime minDate, DateTime maxDate)
        {
            try
            {
                _logger?.LogInformation("Getting existing activities from SP");

                var spListActivities = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spListActivities == null) throw new Exception("Getting SP activity list failed");

                var spListMentors = web.Lists.TryGetList(Constants.Lists.Mentors);
                if (spListMentors == null) throw new Exception("Getting SP mentors list failed");

                var spListRootMentors = web.Lists.TryGetList(Constants.Lists.RootMentors);
                if (spListRootMentors == null) throw new Exception("Getting SP root mentor list failed");

                var spActivities = new List<SpActivity>();

                var dateRangeQuery = new SPQuery
                {
                    Query =
                        $"<Where><And><Geq><FieldRef Name=\"Date\"/><Value Type=\"DateTime\">{minDate:yyyy-MM-dd}</Value></Geq><Leq><FieldRef Name=\"Date\"/><Value Type=\"DateTime\">{maxDate:yyyy-MM-dd}</Value></Leq></And></Where>"
                };

                foreach (var item in spListActivities.GetItems(dateRangeQuery).Cast<SPListItem>())
                {
                    var mentor = SPHelper.GetLookUpUserValue(spListMentors, item, Constants.Activity.Mentor, Constants.Activity.Employee);
                    var mentorValue = SPHelper.GetLookUpItemId(spListMentors, item, Constants.Activity.Mentor,
                        Constants.Activity.Employee);

                    var rootMentor = SPHelper.GetLookUpUserValue(spListRootMentors, item, Constants.Activity.RootMentor, Constants.Activity.Employee);
                    var rootMentorValue = SPHelper.GetLookUpItemId(spListRootMentors, item, Constants.Activity.RootMentor, Constants.Activity.Employee);

                    var month = SPHelper.GetIntValue(item, Constants.Activity.Month);
                    var year = SPHelper.GetIntValue(item, Constants.Activity.Year);

                    var activities = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Activities);
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    spActivities.Add(new SpActivity
                    {
                        RootMentorId = rootMentor?.User.ID,
                        RootMentorLookupId = rootMentorValue,
                        MentorId = mentor?.User.ID,
                        MentorLookupId = mentorValue,
                        Month = month,
                        Year = year,
                        Activities = new List<string>(activities),
                        Paths = new List<string>(paths),
                        Id = item.ID,
                    });
                }

                return spActivities;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing activities from SP failed. {e.Message}");
            }
        }

        void UpdateSpActivities(IEnumerable<SpActivity> itemsToUpdate, SPWeb web)
        {
            try
            {
                _logger?.LogInformation("Updating SP activities list");

                var spList = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spList == null) throw new Exception("Getting SP list failed");

                foreach (var item in itemsToUpdate)
                {
                    if (item.IsNew)
                    {
                        InsertActivity(spList, item);
                    }
                    else
                    {
                        UpdateActivity(spList, item);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Updating SP activities list failed. {e.Message}");
            }
        }

        private void InsertActivity(SPList spList, SpActivity item)
        {
            var newItem = spList.Items.Add();

            if (item.MentorId != null)
            {
                newItem[Constants.Activity.Mentor] = item.MentorLookupId;
            }

            if (item.RootMentorId != null)
            {
                newItem[Constants.Activity.RootMentor] = item.RootMentorLookupId;
            }

            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;

            SPHelper.SetMultiChoiceValue(newItem, Constants.Activity.Activities, item.Activities);
            SPHelper.SetMultiChoiceValue(newItem, Constants.Activity.Paths, item.Paths);

            newItem.Update();
        }

        private void UpdateActivity(SPList spList, SpActivity item)
        {
            var itemToUpdate = spList.Items.GetItemById(item.Id);

            SPHelper.SetMultiChoiceValue(itemToUpdate, Constants.Activity.Activities, item.Activities);
            SPHelper.SetMultiChoiceValue(itemToUpdate, Constants.Activity.Paths, item.Paths);

            itemToUpdate.Update();
        }
    }
}
