using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Helpers;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SPMember
    {
        public int UserId { get; set; }

        public string UserEmail { get; set; }

        public int? MentorLookupId { get; set; }

        public int? RootMentorLookupId { get; set; }

        public IEnumerable<string> Paths { get; set; }

        public override int GetHashCode()
        {
            return UserId;
        }

        public override bool Equals(object obj)
        {
            if (obj is SPMember otherObj)
            {
                return UserId == otherObj.UserId &&
                       MentorLookupId == otherObj.MentorLookupId &&
                       RootMentorLookupId == otherObj.RootMentorLookupId &&
                       (Paths == null && otherObj.Paths == null || Paths.SequenceEqual(otherObj.Paths));
            }

            return false;
        }

        public static IEnumerable<SPMember> GetSPMembers(SPWeb web)
        {
            try
            {
                var spListMentors = web.Lists.TryGetList(Constants.Lists.Mentors);
                if (spListMentors == null) throw new Exception("Getting SP mentors list failed");

                var spListRootMentors = web.Lists.TryGetList(Constants.Lists.RootMentors);
                if (spListRootMentors == null) throw new Exception("Getting SP root mentor list failed");

                var members = new List<SPMember>();

                foreach (var item in spListMentors.GetItems().Cast<SPListItem>())
                {
                    var mentor = SPHelper.GetUserValue(item, Constants.Activity.Employee);
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    members.Add(new SPMember
                    {
                        UserId = mentor.User.ID,
                        MentorLookupId = item.ID,
                        Paths = new List<string>(paths)
                    });
                }

                foreach (var item in spListRootMentors.GetItems().Cast<SPListItem>())
                {
                    var rootMentor = SPHelper.GetUserValue(item, Constants.Activity.Employee);
                    var paths = SPHelper.GetMultiChoiceValue(item, Constants.Activity.Paths);

                    var member = members.FirstOrDefault(x => x.UserId == rootMentor.User.ID);

                    if (member != null)
                    {
                        member.RootMentorLookupId = item.ID;
                    }
                    else
                    {
                        members.Add(new SPMember
                        {
                            UserId = rootMentor.User.ID,
                            RootMentorLookupId = item.ID,
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
    }
}
