using System.Runtime.Serialization;

namespace Test.Activities.Automation.ActivityLib.Sources
{
    public partial class GitLabActivitySource
    {
        [DataContract]
        private protected class Commit
        {
            [DataMember(Name = "author_email")] public string AuthorEmail { get; set; }

            [DataMember(Name = "created_at")] public string Date { get; set; }
        }
    }
}
