using System;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.TimerJob
{
    public class Repository
    {
        public string Host { get; set; }

        public string Activity { get; set; }

        public string ProjectId { get; set; }
    }

    [DataContract]
    public class Commit
    {
        [DataMember(Name = "author_email")]
        public string AuthorEmail { get; set; }
    }

}