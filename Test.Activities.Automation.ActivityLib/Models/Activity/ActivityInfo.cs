using System;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.ActivityLib.Models
{
    [DataContract]
    public class ActivityInfo
    {
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Activity { get; set; }
    }
}
