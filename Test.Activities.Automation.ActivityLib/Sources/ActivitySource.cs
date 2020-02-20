using System.Collections.Generic;
using Test.Activities.Automation.ActivityLib.Models;

namespace Test.Activities.Automation.ActivityLib.Sources
{
    public abstract class ActivitySource
    {
        protected ILogger _logger;

        protected ActivitySource(ILogger logger)
        {
            _logger = logger;

            Configure();
        }

        public virtual void Configure()
        {
        }

        public abstract IEnumerable<ActivityInfo> FetchActivities();
    }
}
