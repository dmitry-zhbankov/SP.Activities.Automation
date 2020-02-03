using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Test.Activities.Automation.ActivityLib
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
