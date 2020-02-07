using System.Collections.Generic;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class SpActivity
    {
        public int Id { get; set; }

        public SPUser User { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public List<string> Activities { get; set; }

        public bool IsNew { get; set; }

        public bool IsNewActivity { get; set; }
    }
}
