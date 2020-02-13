namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services
{
    public partial class EnsureService
    {
        private protected class ActivityKey
        {
            public int UserId { get; set; }

            public int Year { get; set; }

            public int Month { get; set; }

            public override int GetHashCode()
            {
                return UserId ^ Year ^ Month;
            }

            public override bool Equals(object obj)
            {
                if (obj is ActivityKey otherObj)
                {
                    return UserId == otherObj.UserId && Year == otherObj.Year && Month == otherObj.Month;
                }

                return false;
            }
        }
    }
}
