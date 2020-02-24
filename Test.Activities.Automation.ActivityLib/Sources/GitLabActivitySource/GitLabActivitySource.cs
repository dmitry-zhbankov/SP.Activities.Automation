using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.SharePoint;
using Test.Activities.Automation.ActivityLib.Helpers;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.ActivityLib.Sources
{
    public partial class GitLabActivitySource : ActivitySource
    {
        private IEnumerable<SpMember> _members;
        private SPList _configList;

        public GitLabActivitySource(ILogger logger, IEnumerable<SpMember> members, SPList configList) : base(logger)
        {
            _members = members;
            _configList = configList;
        }

        private IEnumerable<Repository> GetConfigRepositories(SPList configList)
        {
            try
            {
                var configItems = configList.GetItems().Cast<SPListItem>();

                var repositories = configItems.Where(item =>
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

                return repositories;
            }
            catch (Exception e)
            {
                throw new Exception($"Getting repositories configuration failed. {e}");
            }
        }

        public override IEnumerable<ActivityInfo> FetchActivities()
        {
            _logger.LogInformation("Fetching GitLab activity");

            try
            {
                var repos = GetConfigRepositories(_configList);
                GetRepoCommits(repos);

                var emailActivities = CreateRepoActivities(repos);
                var activities = emailActivities.Select(x => new ActivityInfo(x, _members)).ToList();

                return activities;
            }
            catch (Exception e)
            {
                _logger.LogError($"Fetching GitLab activity failed. {e}");
                return null;
            }
        }

        private IEnumerable<ActivityInfoEmail> CreateRepoActivities(IEnumerable<Repository> repositories)
        {
            var activities = new List<ActivityInfoEmail>();
            foreach (var repo in repositories)
                foreach (var branch in repo.Branches)
                    activities.AddRange(branch.Commits.Select(commit =>
                        new ActivityInfoEmail
                        {
                            Activity = repo.Activity,
                            Date = DateTime.Parse(commit.Date),
                            UserEmail = commit.AuthorEmail,
                            Paths = repo.Paths
                        }));

            return activities.GroupBy(x => x.UserEmail).Select(x => x.First());
        }

        private void GetRepoCommits(IEnumerable<Repository> repositories)
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
                                branch.Commits = commits.ToList();
                            }

                            repo.Branches = new List<Branch>(branches);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Getting '{repo.Host}' repository commits failed. {e}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Getting repository commits failed. {e}");
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
                throw new Exception($"Getting branch commits failed. {e}");
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
                throw new Exception($"Getting branches failed. {e}");
            }
        }
    }
}
