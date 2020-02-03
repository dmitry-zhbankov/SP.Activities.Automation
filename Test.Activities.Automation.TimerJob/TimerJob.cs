using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Microsoft.SharePoint;
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
            var devActivities = GetDevActivities();

            var mentoringActivities = GetMentoringActivities();

            SendActivities(devActivities.Concat(mentoringActivities));
        }

        private void SendActivities(IEnumerable<ActivityInfo> activities)
        {
            activities = new List<ActivityInfo>()
            {
                new ActivityInfo
                {
                    UserEmail = "1@com",
                    Activity = "Development",
                    Date = DateTime.Now
                },
                new ActivityInfo
                {
                    UserEmail = "2@com",
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

            var content = new StringContent(str, Encoding.UTF8, Constants.HttpHeader.MediaType.ApplicatonJson);
            var uri = new Uri(Constants.ServiceUrl);

            var res = svcClient.PostAsync(uri, content).Result;
        }

        private IEnumerable<ActivityInfo> GetMentoringActivities()
        {
            var activities = new List<ActivityInfo>();

            return activities;
        }

        private IEnumerable<ActivityInfo> GetDevActivities()
        {
            var activities = new List<ActivityInfo>();

            var repositories = GetConfigRepositories();

            GetGitLabCommits(repositories);

            return activities;
        }

        private void GetGitLabCommits(IEnumerable<Repository> repositories)
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(Constants.GitLab.PrivateToken, Constants.GitLab.Token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.HttpHeader.MediaType.ApplicatonJson));

                var jsonSerializer = new DataContractJsonSerializer(typeof(Commit[]));

                foreach (var repo in repositories)
                {
                    var branchUrl =
                        $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Branches}";
                    var branchResponse = client.GetAsync(branchUrl).Result;
                    var stream = branchResponse.Content.ReadAsStreamAsync().Result;
                    stream.Position = 0;
                    var reader = new StreamReader(stream);
                    var strCommits = reader.ReadToEnd();
                    stream.Position = 0;

                    var branches = jsonSerializer.ReadObject(stream) as Branch[];
                    if (branches == null)
                    {
                        continue;
                    }

                    foreach (var branch in branches)
                    {
                        var commitUrl =
                            $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.CommitsSince}{DateTime.Now.AddDays(-7).Date:O}";
                        var commitResponse = client.GetAsync(commitUrl).Result;
                        stream.SetLength(0);
                        stream = commitResponse.Content.ReadAsStreamAsync().Result;
                        stream.Position = 0;

                        var commits = jsonSerializer.ReadObject(stream) as Commit[];
                        if (commits == null)
                        {
                            continue;
                        }

                        branch.Commits.AddRange(commits);
                        repo.Branches.Add(branch);
                    }

                }
            }
        }

        private IEnumerable<Repository> GetConfigRepositories()
        {
            SPListItemCollection configItems;

            using (var site = new SPSite(Constants.Host))
            using (var web = site.OpenWeb(Constants.Web))
            {
                var configList = web.Lists.TryGetList(Constants.Lists.Configurations);
                configItems = configList?.GetItems();
            }

            return (configItems?.Cast<SPListItem>()
                .Where(item => item[Constants.Configuration.Key] as string == Constants.Configuration.ConfigurationKeys.GitLabRepository)
                .Select(item => item[Constants.Configuration.Value] as string)
                .Select(value => value?.Split(new[] { Constants.Configuration.Separator }, StringSplitOptions.RemoveEmptyEntries))
                .Select(str => new Repository() { Host = str?[0], ProjectId = str?[1], Activity = str?[2] }))?.ToList();
        }
    }
}
