using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.WCFService
{
    [DataContract]
    public class ActivityCollection
    {
        [DataMember]
        
        public object[] Activities { get; set; }
    }
}