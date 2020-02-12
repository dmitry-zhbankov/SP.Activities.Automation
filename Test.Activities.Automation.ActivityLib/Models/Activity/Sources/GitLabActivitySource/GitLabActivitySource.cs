using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Models.Helpers;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public partial class GitLabActivitySource : ActivitySource
    {
        private IEnumerable<Repository> _repositories;

        public GitLabActivitySource(ILogger logger) : base(logger)
        {
        }

        public override void Configure()
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

                _repositories = configItems.Where(item =>
                         item[Constants.Configuration.Key] as string ==
                         Constants.Configuration.ConfigurationKeys.GitLabRepository)
                    .Select(item => item[Constants.Configuration.Value] as string)
                    .Select(value =>
                        value.Split(new[] { Constants.Configuration.Separator }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(str =>
                        new Repository
                        {
                            Host = str[0],
                            ProjectId = str[1],
                            Activity = str[2],
                            Paths = new List<string>(str[3].Split(';'))
                        })
                    .ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"Getting repositories configuration failed. {e.Message}");
            }
        }

        public override IEnumerable<ActivityInfo> FetchActivity()
        {
            _logger.LogInformation("Fetching GitLab activity");

            try
            {
                GetRepoCommits();

                var activities = CreateRepoActivities();

                return activities;
            }
            catch (Exception e)
            {
                _logger.LogError($"Fetching GitLab activity failed. {e.Message}");
                return null;
            }
        }

        private IEnumerable<ActivityInfo> CreateRepoActivities()
        {
            var activities = new List<ActivityInfo>();
            foreach (var repo in _repositories)
                foreach (var branch in repo.Branches)
                    activities.AddRange(branch.Commits.Select(commit =>
                        new ActivityInfo
                        {
                            Activity = repo.Activity,
                            Date = DateTime.Parse(commit.Date),
                            UserEmail = commit.AuthorEmail,
                            Paths = repo.Paths
                        }));

            return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
        }

        private void GetRepoCommits()
        {
            try
            {
                using (var handler = new HttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add(Constants.GitLab.PrivateToken, Constants.GitLab.Token);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue(Constants.HttpHeader.MediaType.ApplicationJson));

                    foreach (var repo in _repositories)
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

        private IEnumerable<Commit> GetBranchCommits(HttpClient client, Branch branch, Repository repo)
        {
            try
            {
                var url = string.Concat(
                    new[]
                    {
                        $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Commits}",
                        $"?{Constants.GitLab.Branch}{branch.Name}",
                        $"&{Constants.GitLab.Since}{DateTime.Now.AddDays(-1).Date:yyyy-MM-dd}",
                        $"&{Constants.GitLab.Until}{DateTime.Now.Date:yyyy-MM-dd}"
                    }
                );

                return APIHelper.GetApiCollection<Commit>(url, client);
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

                return APIHelper.GetApiCollection<Branch>(url, client);
            }
            catch (Exception e)
            {
                throw new Exception($"Getting branches failed. {e.Message}");
            }
        }
    }
}
