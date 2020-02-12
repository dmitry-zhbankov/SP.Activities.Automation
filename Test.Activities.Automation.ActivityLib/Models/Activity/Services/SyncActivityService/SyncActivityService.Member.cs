using System.Collections.Generic;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.SyncActivityService
{
    public partial class SyncActivityService
    {
        private protected class Member
        {
            public SPUser Mentor { get; set; }

            public SPUser RootMentor { get; set; }

            public IEnumerable<string> Paths { get; set; }
        }
    }
}
