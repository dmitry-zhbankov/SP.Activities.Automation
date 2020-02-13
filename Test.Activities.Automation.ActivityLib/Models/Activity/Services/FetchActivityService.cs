using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class FetchActivityService
    {
        private ILogger _logger;

        public FetchActivityService(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<ActivityInfo> FetchActivities()
        {
            _logger?.LogInformation("Fetching activities");

            var devActivities = GetDevActivities();

            var mentoringActivities = GetMentoringActivities();

            return devActivities.Concat(mentoringActivities).ToList();
        }

        public IEnumerable<ActivityInfo> GetDevActivities()
        {
            try
            {
                _logger?.LogInformation("Getting dev activities");

                var devActivitySource = new GitLabActivitySource(_logger);

                return devActivitySource.FetchActivity();
            }
            catch (Exception e)
            {
                _logger?.LogError($"Getting dev activities failed. {e.Message}");
                return new List<ActivityInfo>();
            }
        }

        public IEnumerable<ActivityInfo> GetMentoringActivities()
        {
            try
            {
                _logger?.LogInformation("Getting mentoring activities");

                var mentoringActivitySource = new SPCalendarActivitySource(_logger);

                return mentoringActivitySource.FetchActivity();
            }
            catch (Exception e)
            {
                _logger?.LogError($"Getting mentoring activities failed. {e.Message}");
                return new List<ActivityInfo>();
            }
        }
    }
}
