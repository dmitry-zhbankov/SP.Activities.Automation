using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Test.Activities.Automation.ActivityLib.Sources
{
    public partial class GitLabActivitySource
    {
        [DataContract]
        private protected class Branch
        {
            [DataMember(Name = "name")] public string Name { get; set; }

            public List<Commit> Commits { get; set; }
        }
    }
}
