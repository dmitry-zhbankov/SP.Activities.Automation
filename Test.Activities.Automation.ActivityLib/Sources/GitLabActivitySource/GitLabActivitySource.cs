using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Test.Activities.Automation.ActivityLib.Helpers;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Utils.Constants;
using SPMember = Test.Activities.Automation.ActivityLib.Models.SPMember;

namespace Test.Activities.Automation.ActivityLib.Sources
{
    public partial class GitLabActivitySource : ActivitySource
    {
        private IEnumerable<SPMember> _members;
        private SPWeb _web;

        public GitLabActivitySource(ILogger logger, IEnumerable<SPMember> members, SPWeb web) : base(logger)
        {
            _members = members;
            _web = web;
        }

        private static IEnumerable<Repository> GetConfigRepositories(SPList configList)
        {
            try
            {
                var spQuery = new SPQuery
                {
                    Query =
                        "<Where>" +
                            "<Eq>" +
                                $"<FieldRef Name=\"{Constants.Configuration.Key}\"/>" +
                                    $"<Value Type=\"Text\">{Constants.Configuration.ConfigurationKeys.GitLabRepository}</Value>" +
                            "</Eq>" +
                        "</Where>"
                };

                var repositories = configList.GetItems(spQuery).Cast<SPListItem>()
                    .Where(item =>
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
                var configList = _web.Lists[Constants.Lists.Configurations];
                if (configList == null) throw new Exception("Getting SP config list failed");

                var repos = GetConfigRepositories(configList);
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

        private static IEnumerable<ActivityInfoEmail> CreateRepoActivities(IEnumerable<Repository> repositories)
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

        private static IEnumerable<Commit> GetBranchCommits(HttpClient client, Branch branch, Repository repo)
        {
            try
            {
                var url = string.Concat(
                    new[]
                    {
                        $"{repo.Host}{Constants.GitLab.Api}/{repo.ProjectId}/{Constants.GitLab.Commits}",
                        $"?{Constants.GitLab.Branch}{branch.Name}",
                        $"&{Constants.GitLab.Since}{SPUtility.CreateISO8601DateTimeFromSystemDateTime(DateTime.Now.AddDays(-1).Date)}",
                        $"&{Constants.GitLab.Until}{SPUtility.CreateISO8601DateTimeFromSystemDateTime(DateTime.Now.Date)}"
                    }
                );

                return APIHelper.GetApiCollection<Commit>(url, client);
            }
            catch (Exception e)
            {
                throw new Exception($"Getting branch commits failed. {e}");
            }
        }

        private static IEnumerable<Branch> GetBranches(HttpClient client, Repository repo)
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
