using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
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

            SendActivities(devActivities.Concat(mentoringActivities).ToList());
        }

        private void SendActivities(IEnumerable<ActivityInfo> activities)
        {
            var serializer = new DataContractJsonSerializer(typeof(List<ActivityInfo>));
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
            var repositories = GetConfigRepositories();

            GetGitLabCommits(repositories);

            return CreateRepoActivities(repositories);
        }

        private IEnumerable<ActivityInfo> CreateRepoActivities(IEnumerable<Repository> repositories)
        {
            var activities = new List<ActivityInfo>();
            foreach (var repo in repositories)
            {
                foreach (var branch in repo.Branches)
                {
                    activities.AddRange(branch.Commits.Select(commit => new ActivityInfo {Activity = repo.Activity, Date = DateTime.Parse(commit.Date), UserEmail = commit.AuthorEmail}));
                }
            }

            return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
        }

        private void GetGitLabCommits(IEnumerable<Repository> repositories)
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(Constants.GitLab.PrivateToken, Constants.GitLab.Token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.HttpHeader.MediaType.ApplicatonJson));

                foreach (var repo in repositories)
                {
                    var branchUrl =
                        $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Branches}";
                    var branchResponse = client.GetAsync(branchUrl).Result;


                    var branches = JsonDeserialize<Branch[]>(branchResponse, branchUrl);

                    if (branches == null)
                    {
                        var br = JsonDeserialize<Branch>(branchResponse, branchUrl);
                        if (br == null)
                        {
                            continue;
                        }
                        branches=new[]
                        {
                            br
                        };
                    }

                    repo.Branches=new List<Branch>();

                    foreach (var branch in branches)
                    {
                        branch.Commits=new List<Commit>();

                        var commitUrl =
                            $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Commits}/{branch.Name}{Constants.GitLab.Since}{DateTime.Now.AddDays(-1).Date:yyyy-MM-dd}";
                        var commitResponse = client.GetAsync(commitUrl).Result;

                        var commits = JsonDeserialize<Commit[]>(commitResponse, commitUrl);
                        if (commits == null)
                        {
                            var commit = JsonDeserialize<Commit>(commitResponse, commitUrl);
                            if (commit==null)
                            {
                                continue;
                            }
                            branch.Commits.Add(commit);
                        }
                        else
                        {
                            branch.Commits.AddRange(commits);
                        }
                        
                        repo.Branches.Add(branch);
                    }
                }
            }
        }

        T JsonDeserialize<T>(HttpResponseMessage httpResponse, string url) where  T: class
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            var stream = httpResponse.Content.ReadAsStreamAsync().Result;
            stream.Position = 0;
            var res = serializer.ReadObject(stream) as T;
            return res;
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
