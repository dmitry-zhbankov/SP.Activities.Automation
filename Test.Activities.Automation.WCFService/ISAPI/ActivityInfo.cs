using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.WCFService
{
    [DataContract]
    public class ActivityInfo
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Activity { get; set; }
    }
}
