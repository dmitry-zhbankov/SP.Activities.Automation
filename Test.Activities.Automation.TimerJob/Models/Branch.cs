using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.TimerJob.Models
{
    [DataContract]
    public class Branch
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        public List<Commit> Commits { get; set; }
    }
}