using System.Collections.Generic;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public partial class GitLabActivitySource
    {
        private protected class Repository
        {
            public string Host { get; set; }

            public string Activity { get; set; }

            public string ProjectId { get; set; }

            public List<Branch> Branches { get; set; }

            public List<string> Paths { get; set; }
        }
    }
}
