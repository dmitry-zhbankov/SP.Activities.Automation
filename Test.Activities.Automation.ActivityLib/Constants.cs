using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Activities.Automation.ActivityLib
{
    public static class Constants
    {
        public const string ServiceUrl = @"http://localhost/_vti_bin/AutomationService.svc/FillActivities";
        
        public static class Activities
        {
            public const string Mentoring = "Mentoring";
            public const string Development1 = "Development 1";
            public const string Development2 = "Development 2";
        }

        public static class Lists
        {
            public const string MentoringCalendar = "MentoringCalendar";
            public const string Configurations = "Configurations";
        }

        public static class ConfigurationKeys
        {
            public const string GitLabRepository = "GitLabRepository";
        }

        public static class GitLab
        {
            public const string Api = "api/v4/projects";
            public const string Commits = "repository/commits";
        }
    }
}
