using System.Collections.Generic;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Classes
{
    public class Member
    {
        public int? MentorId { get; set; }

        public int? MentorLookupId { get; set; }

        public string MentorEmail { get; set; }

        public int? RootMentorId { get; set; }

        public int? RootMentorLookupId { get; set; }

        public string RootMentorEmail { get; set; }

        public IEnumerable<string> Paths { get; set; }
    }
}
