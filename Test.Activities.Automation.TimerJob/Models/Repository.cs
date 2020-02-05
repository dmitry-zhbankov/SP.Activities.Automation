using System.Collections.Generic;

namespace Test.Activities.Automation.TimerJob.Models
{
    public class Repository
    {
        public string Host { get; set; }

        public string Activity { get; set; }

        public string ProjectId { get; set; }

        public List<Branch> Branches { get; set; }
    }
}