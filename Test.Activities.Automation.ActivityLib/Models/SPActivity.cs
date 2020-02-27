using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Test.Activities.Automation.ActivityLib.Helpers;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SPActivity
    {
        public int SPActivityId { get; set; }

        public SPMember SPMember { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public List<string> Activities { get; set; }

        public List<string> Paths { get; set; }

        public bool IsNew => SPActivityId == default;

        public override int GetHashCode()
        {
            return SPActivityId;
        }

        public override bool Equals(object obj)
        {
            if (obj is SPActivity otherObj)
            {
                return SPMember.Equals(otherObj.SPMember) &&
                       Year == otherObj.Year &&
                       Month == otherObj.Month &&
                       (Paths == null && otherObj.Paths == null || Paths.SequenceEqual(otherObj.Paths)) &&
                       (Activities == null && otherObj.Activities == null || Activities.SequenceEqual(otherObj.Activities));
            }

            return false;
        }

        public static IEnumerable<SPActivity> GetSPActivities(SPWeb web, IEnumerable<SPMember> spMembers, DateTime minDate, DateTime maxDate)
        {
            try
            {
                var spListActivities = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spListActivities == null) throw new Exception("Getting SP activity list failed");

                var strMinDate = SPUtility.CreateISO8601DateTimeFromSystemDateTime(minDate);
                var strMaxDate = SPUtility.CreateISO8601DateTimeFromSystemDateTime(maxDate);
                var dateRangeQuery = new SPQuery
                {
                    Query =
                        "<Where>" +
                            "<And>" +
                                "<Geq>" +
                                    $"<FieldRef Name=\"{Constants.Activity.Date}\"/>" +
                                        $"<Value Type=\"DateTime\">{strMinDate}</Value>" +
                                "</Geq>" +
                                "<Leq>" +
                                    $"<FieldRef Name=\"{Constants.Activity.Date}\"/>" +
                                        $"<Value Type=\"DateTime\">{strMaxDate}</Value>" +
                                "</Leq>" +
                            "</And>" +
                        "</Where>"
                };

                var spActivities = new List<SPActivity>();

                foreach (var item in spListActivities.GetItems(dateRangeQuery).Cast<SPListItem>())
                {
                    var rootMentorLookupId = SPHelper.GetItemLookupId(item, Constants.Calendar.RootMentor);
                    var mentorLookupId = SPHelper.GetItemLookupId(item, Constants.Calendar.Mentor);

                    var member = spMembers.FirstOrDefault(x => x.MentorLookupId == mentorLookupId && x.RootMentorLookupId == rootMentorLookupId);

                    var month = SPHelper.GetIntValue(item, Constants.Activity.Month);
                    var year = SPHelper.GetIntValue(item, Constants.Activity.Year);

                    var activities = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Activities);
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    if (member == null) continue;

                    spActivities.Add(new SPActivity
                    {
                        SPMember = member,
                        Month = month,
                        Year = year,
                        Activities = new List<string>(activities),
                        Paths = new List<string>(paths),
                        SPActivityId = item.ID
                    });
                }

                return spActivities;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting existing activities from SP failed. {e.Message}");
            }
        }

        public static void UpdateSPActivities(IEnumerable<SPActivity> itemsToUpdate, SPWeb web)
        {
            try
            {
                var spListActivities = web.Lists.TryGetList(Constants.Lists.Activities);
                if (spListActivities == null) throw new Exception("Getting SP activity list failed");

                foreach (var item in itemsToUpdate)
                {
                    if (item.IsNew)
                    {
                        InsertSPActivity(spListActivities, item);
                    }
                    else
                    {
                        UpdateSPActivity(spListActivities, item);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Updating SP activities list failed. {e.Message}");
            }
        }

        public static void InsertSPActivity(SPList spList, SPActivity item)
        {
            var newItem = spList.Items.Add();

            newItem[Constants.Activity.Mentor] = item.SPMember.MentorLookupId;
            newItem[Constants.Activity.RootMentor] = item.SPMember.RootMentorLookupId;
            newItem[Constants.Activity.Month] = item.Month;
            newItem[Constants.Activity.Year] = item.Year;

            SPHelper.SetMultiChoiceValue(newItem, Constants.Activity.Activities, item.Activities);
            SPHelper.SetMultiChoiceValue(newItem, Constants.Activity.Paths, item.Paths);

            newItem.Update();
        }

        public static void UpdateSPActivity(SPList spList, SPActivity item)
        {
            var itemToUpdate = spList.Items.GetItemById(item.SPActivityId);

            SPHelper.SetMultiChoiceValue(itemToUpdate, Constants.Activity.Activities, item.Activities);
            SPHelper.SetMultiChoiceValue(itemToUpdate, Constants.Activity.Paths, item.Paths);

            itemToUpdate.Update();
        }
    }
}
