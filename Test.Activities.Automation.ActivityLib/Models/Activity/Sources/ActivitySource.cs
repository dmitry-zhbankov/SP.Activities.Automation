using System.Collections.Generic;

namespace Test.Activities.Automation.ActivityLib.Models
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

        public abstract IEnumerable<InfoActivity> FetchActivity();
    }
}
