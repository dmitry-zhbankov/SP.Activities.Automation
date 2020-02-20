using System.Collections.Generic;
using Test.Activities.Automation.ActivityLib.Models;

namespace Test.Activities.Automation.ActivityLib.Services
{
    public partial class EnsureService
    {
        private protected class ActivityValue
        {
            public int ActivityId { get; set; }

            public SpMember SpMember { get; set; }

            public SortedSet<string> Paths { get; set; }

            public SortedSet<string> Activities { get; set; }

            public bool IsModified { get; set; }
        }
    }
}
