using System.Collections.Generic;
using Microsoft.SharePoint;

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
    }
}
