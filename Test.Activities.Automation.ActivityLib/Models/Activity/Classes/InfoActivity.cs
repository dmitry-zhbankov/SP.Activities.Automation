using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.ActivityLib.Models
{
    [DataContract]
    public class InfoActivity
    {
        public int UserId { get; set; }

        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Activity { get; set; }

        public IEnumerable<string> Paths { get; set; }
    }
}
