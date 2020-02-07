using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class RepositoryService
    {
        private ILogger _logger;

        public IEnumerable<Repository> GetConfigRepositories()
        {
            try
            {
                IEnumerable<SPListItem> configItems;

                using (var site = new SPSite(Constants.Host))
                using (var web = site.OpenWeb(Constants.Web))
                {
                    var configList = web.Lists[Constants.Lists.Configurations];
                    configItems = configList.GetItems().Cast<SPListItem>();
                }

                return configItems.Where(item =>
                        item[Constants.Configuration.Key] as string ==
                        Constants.Configuration.ConfigurationKeys.GitLabRepository)
                    .Select(item => item[Constants.Configuration.Value] as string)
                    .Select(value =>
                        value.Split(new[] { Constants.Configuration.Separator }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(str => new Repository { Host = str[0], ProjectId = str[1], Activity = str[2] })
                    .ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"Getting repositories configuration failed. {e.Message}");
            }
        }

        public void GetRepoCommits(IEnumerable<Repository> repositories)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add(Constants.GitLab.PrivateToken, Constants.GitLab.Token);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue(Constants.HttpHeader.MediaType.ApplicationJson));

                    foreach (var repo in repositories)
                    {
                        try
                        {
                            var branches = GetBranches(client, repo);

                            foreach (var branch in branches)
                            {
                                var commits = GetBranchCommits(client, branch, repo);

                                branch.Commits = new List<Commit>(commits);
                            }

                            repo.Branches = new List<Branch>(branches);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Getting '{repo.Host}' repository commits failed. {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Getting repository commits failed. {e.Message}");
            }
        }

        public IEnumerable<Commit> GetBranchCommits(HttpClient client, Branch branch, Repository repo)
        {
            try
            {
                var url =
                    $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Commits}" +
                    $"?{Constants.GitLab.Branch}{branch.Name}" +
                    $"&{Constants.GitLab.Since}{DateTime.Now.AddDays(-1).Date:yyyy-MM-dd}" +
                    $"&{Constants.GitLab.Until}{DateTime.Now.Date:yyyy-MM-dd}";

                return GetApiCollection<Commit>(url, client);
            }
            catch (Exception e)
            {
                throw new Exception($"Getting branch commits failed. {e.Message}");
            }
        }

        private IEnumerable<Branch> GetBranches(HttpClient client, Repository repo)
        {
            try
            {
                var url =
                    $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Branches}";

                return GetApiCollection<Branch>(url, client);
            }
            catch (Exception e)
            {
                throw new Exception($"Getting branches failed. {e.Message}");
            }
        }

        private IEnumerable<T> GetApiCollection<T>(string url, HttpClient client) where T : class
        {
            var response = client.GetAsync(url).Result;
            var stream = response.Content.ReadAsStreamAsync().Result;

            var res = new List<T>();

            var reader = new StreamReader(stream);
            stream.Position = 0;

            var ch = reader.Read();
            if (ch == '[')
            {
                var multiple = JsonDeserialize<T[]>(stream);
                res.AddRange(multiple);

                return res;
            }

            var single = JsonDeserialize<T>(stream);
            res.Add(single);

            return res;
        }

        private T JsonDeserialize<T>(Stream stream) where T : class
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            stream.Position = 0;
            var res = serializer.ReadObject(stream) as T;
            return res;
        }
    }
}
