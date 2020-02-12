using System.Collections.Generic;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.SyncActivityService
{
    public partial class SyncActivityService
    {
        private protected class ActivityValue
        {
            public int ActivityId { get; set; }

            public int? MentorId { get; set; }

            public int? MentorLookupId { get; set; }

            public int? RootMentorId { get; set; }

            public int? RootMentorLookupId { get; set; }

            public HashSet<string> Paths { get; set; }

            public HashSet<string> Activities { get; set; }

            public bool IsModified { get; set; }
        }
    }
}
