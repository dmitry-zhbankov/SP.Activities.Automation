using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.ActivityLib.Models
{
    [DataContract]
    public class ActivityInfo
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Activity { get; set; }
        
        [DataMember]
        public IEnumerable<string> Paths { get; set; }

        public ActivityInfo()
        {
        }

        public ActivityInfo(ActivityInfoEmail activityInfoEmail, IEnumerable<SpMember> members)
        {
            UserId = GetUserIdByEmail(activityInfoEmail.UserEmail, members);
            Date = activityInfoEmail.Date;
            Activity = activityInfoEmail.Activity;
            Paths = activityInfoEmail.Paths;
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(UserId);
        }

        public override bool Equals(object obj)
        {
            if (obj is ActivityInfo otherObj)
            {
                return UserId == otherObj.UserId &&
                       Date.Equals(otherObj.Date) &&
                       Activity == otherObj.Activity &&
                       (Paths?.SequenceEqual(otherObj.Paths) ?? otherObj.Paths == null);
            }

            return false;
        }

        private static int GetUserIdByEmail(string email, IEnumerable<SpMember> members)
        {
            var user = members.FirstOrDefault(x => x.UserEmail == email);

            if (user == null) return default;

            var id = user.UserId;
            return id;
        }
    }
}
