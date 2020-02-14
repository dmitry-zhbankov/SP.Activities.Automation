using System.Collections.Generic;
using System.Linq;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SpActivity
    {
        public int Id { get; set; }

        public int? MentorId { get; set; }

        public int? MentorLookupId { get; set; }

        public int? RootMentorId { get; set; }

        public int? RootMentorLookupId { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public List<string> Activities { get; set; }

        public List<string> Paths { get; set; }

        public bool IsNew => Id == 0;

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is SpActivity otherObj)
            {
                return MentorId == otherObj.MentorId &&
                       MentorLookupId == otherObj.MentorLookupId &&
                       RootMentorId == otherObj.RootMentorId &&
                       RootMentorLookupId == otherObj.RootMentorLookupId &&
                       Year == otherObj.Year &&
                       Month == otherObj.Month &&
                       (Paths == null && otherObj.Paths == null || Paths.SequenceEqual(otherObj.Paths)) &&
                       (Activities == null && otherObj.Activities == null || Activities.SequenceEqual(otherObj.Activities));
            }

            return false;
        }
    }
}
