using System.Collections.Generic;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.SyncActivityService
{
    public partial class SyncActivityService
    {
        private protected class ActivityKey : IEqualityComparer<ActivityKey>
        {
            public int UserId { get; set; }

            public int Year { get; set; }

            public int Month { get; set; }

            public bool Equals(ActivityKey x, ActivityKey y)
            {
                return x.UserId == y.UserId && x.Year == y.Year && x.Month == y.Month;
            }

            public int GetHashCode(ActivityKey obj)
            {
                return obj.UserId ^ obj.Year ^ obj.Month;
            }
        }
    }
}
