using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Test.Activities.Automation.ActivityLib;


namespace Test.Activities.Automation.TimerJob
{
    public class TimerJob : SPJobDefinition
    {
        public TimerJob() : base()
        {
        }

        public TimerJob(string name, SPWebApplication webApp, SPServer server, SPJobLockType lockType) : base(name, webApp, server, lockType)
        {
        }

        public override void Execute(Guid targetInstanceId)
        {
            var devActivities= GetDevActivities();

            var mentoringActivities= GetMentoringActivities();

            SendActivities(devActivities.Concat(mentoringActivities));
        }

        private void SendActivities(IEnumerable<ActivityInfo> activities)
        {
            activities = new List<ActivityInfo>()
            {
                new ActivityInfo
                {
                    UserId = 1,
                    Activity = "Development",
                    Date = DateTime.Now
                },
                new ActivityInfo
                {
                    UserId = 2,
                    Activity = Constants.Activities.Mentoring,
                    Date = DateTime.Now
                }
            };
            
            var serializer = new DataContractJsonSerializer(typeof(ActivityInfo[]));
            var ms = new MemoryStream();
            serializer.WriteObject(ms, activities);

            ms.Position = 0;
            var reader = new StreamReader(ms);
            var str = reader.ReadToEnd();

            var svcHandler = new HttpClientHandler
            {
                UseDefaultCredentials = true,
            };
            var svcClient = new HttpClient(svcHandler);

            var content = new StringContent(str, Encoding.UTF8, @"application/json");
            var uri = new Uri(Constants.ServiceUrl);

            var res = svcClient.PostAsync(uri, content).Result;
        }

        private IEnumerable<ActivityInfo> GetMentoringActivities()
        {
            var activities=new List<ActivityInfo>();

            return activities;
        }

        private IEnumerable<ActivityInfo> GetDevActivities()
        {
            var activities=new List<ActivityInfo>();
            
            var repositories = GetConfigRepositories();

            var commits = GetGitLabCommits(repositories);

            return activities;
        }

        private IEnumerable<Commit> GetGitLabCommits(IEnumerable<Repository>repositories)
        {
            Commit[] commits = null;

            using (var handler = new HttpClientHandler())
            {
                using (var gitClient = new HttpClient(handler))
                {
                    gitClient.DefaultRequestHeaders.Add("Private-Token", Constants.GitLab.PrivateToken);
                    gitClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var jsonCommitSerializer = new DataContractJsonSerializer(typeof(Commit[]));

                    foreach (var rep in repositories)
                    {
                        var url =
                            $"{rep.Host}{Constants.GitLab.Api}/{rep.ProjectId}/{Constants.GitLab.CommitsSince}{DateTime.Now.AddDays(-1).Date:yyyy-MM-dd}";
                        var resp = gitClient.GetAsync(url).Result;
                        var stream = resp.Content.ReadAsStreamAsync().Result;
                        stream.Position = 0;
                        var commitsReader = new StreamReader(stream);
                        var strCommits = commitsReader.ReadToEnd();
                        stream.Position = 0;
                        commits = jsonCommitSerializer.ReadObject(stream) as Commit[];
                    }
                }
            }
            return commits;
        }

        private IEnumerable<Repository> GetConfigRepositories()
        {
            var list = new List<Repository>()
            {
                new Repository()
                {
                    Host= "https://gitlab.itechart-group.com/",
                    Activity = "Development 1",
                    ProjectId="539",
                }
            };
            return list;
        }
    }
}
