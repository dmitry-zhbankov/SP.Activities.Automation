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

        private async void SendActivities(IEnumerable<ActivityInfo> activities)
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

            for (int i = 0; i < 3; i++)
            {
                var res = await svcClient.PostAsync(uri, content);
                if (res.StatusCode==HttpStatusCode.OK)
                {
                    break;
                }

                await Task.Delay(new TimeSpan(0, 5, 0));
            }
        }

        private IEnumerable<ActivityInfo> GetMentoringActivities()
        {
            var activities = new List<ActivityInfo>();

            return activities;
        }

        private IEnumerable<ActivityInfo> GetDevActivities()
        {
            var repositories = GetConfigRepositories();

            GetRepoCommits(repositories);

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

        private void GetRepoCommits(IEnumerable<Repository> repositories)
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(Constants.GitLab.PrivateToken, Constants.GitLab.Token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.HttpHeader.MediaType.ApplicatonJson));

                foreach (var repo in repositories)
                {
                    var branches = GetBranches(client, repo);

                    foreach (var branch in branches)
                    {
                        var commits = GetBranchCommits(client, branch, repo);

                        branch.Commits = new List<Commit>(commits);
                    }

                    repo.Branches=new List<Branch>(branches);
                }
            }
        }

        private IEnumerable<Commit> GetBranchCommits(HttpClient client, Branch branch, Repository repo)
        {
            var url =
                $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Commits}/{branch.Name}{Constants.GitLab.Since}{DateTime.Now.AddDays(-1).Date:yyyy-MM-dd}";

            return GetApiCollection<Commit>(url, client);
        }


        private IEnumerable<T> GetApiCollection<T>(string url, HttpClient client)where  T: class
        {
            var response = client.GetAsync(url).Result;

            var array = JsonDeserialize<T[]>(response);
            if (array != null) return array;

            var single = JsonDeserialize<T>(response);
            if (single == null)
            {
                return null;
            }

            array = new[]
            {
                single
            };

            return array;
        }

        private IEnumerable<Branch> GetBranches(HttpClient client, Repository repo)
        {
            var url =
                $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Branches}";

            return GetApiCollection<Branch>(url,client);
        }

        T JsonDeserialize<T>(HttpResponseMessage httpResponse) where  T: class
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
