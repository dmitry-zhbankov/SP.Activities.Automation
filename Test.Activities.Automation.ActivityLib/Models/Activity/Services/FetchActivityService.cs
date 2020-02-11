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

        public IEnumerable<InfoActivity> GetActivities()
        {
            var devActivities = GetDevActivities();

            var mentoringActivities = GetMentoringActivities();

            return devActivities.Concat(mentoringActivities).ToList();
        }
        
        public IEnumerable<InfoActivity> GetDevActivities()
        {
            try
            {
                _logger?.LogInformation("Getting dev activities");

                var devActivitySource=new GitLabActivitySource(_logger);

                return devActivitySource.FetchActivity();
            }
            catch (Exception e)
            {
                _logger?.LogError($"Getting dev activities failed. {e.Message}");
                return new List<InfoActivity>();
            }
        }
        
        public  IEnumerable<InfoActivity> GetMentoringActivities()
        {
            try
            {
                _logger?.LogInformation("Getting mentoring activities");

                var mentoringActivitySource=new SPCalendarActivitySource(_logger);

                return mentoringActivitySource.FetchActivity();
            }
            catch (Exception e)
            {
                _logger?.LogError($"Getting mentoring activities failed. {e.Message}");
                return new List<InfoActivity>();
            }
        }
    }
}
