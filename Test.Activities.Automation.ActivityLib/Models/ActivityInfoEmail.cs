using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.ActivityLib.Models
{
    [DataContract]
    public class ActivityInfoEmail
    {
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Activity { get; set; }

        [DataMember]
        public IEnumerable<string> Paths { get; set; }

        public override int GetHashCode()
        {
            return UserEmail.Length;
        }

        public override bool Equals(object obj)
        {
            if (obj is ActivityInfoEmail otherObj)
            {
                return UserEmail == otherObj.UserEmail &&
                       Date.Equals(otherObj.Date) &&
                       Activity == otherObj.Activity &&
                       (Paths == null && otherObj.Paths == null || Paths.SequenceEqual(otherObj.Paths));
            }

            return false;
        }
    }
}
