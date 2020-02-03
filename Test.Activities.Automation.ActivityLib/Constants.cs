using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Activities.Automation.ActivityLib
{
    public static class Constants
    {
        public const string ServiceUrl = @"http://localhost/_vti_bin/AutomationService.svc/FillActivities";
        public const string Host = "http://localhost";
        public const string Web = "Projects";

        public static class Activities
        {
            public const string Mentoring = "Mentoring";
            public const string Development1 = "Development 1";
            public const string Development2 = "Development 2";
        }

        public static class Lists
        {
            public const string MentoringCalendar = "MentoringCalendar";
            public const string Configurations = "Config";
        }

        public static class Configuration
        {
            public const string Key = "Key";
            public const string Value = "Value";
            public const string Separator = "@";
            public static class ConfigurationKeys
            {
                public const string GitLabRepository = "GitLabRepo";
            }
        }

        public static class GitLab
        {
            public const string Api = "api/v4/projects";
            public const string Since = "?since=";
            public const string Commits = "repository/commits";
            public static readonly string Token = Properties.Resources.Token;
            public const string PrivateToken = "Private-Token";
            public const string Branches = "repository/branches";
        }

        public static class HttpHeader
        {
            public static class MediaType
            {
                public const string ApplicatonJson = "application/json";
            }
        }

        public static class Format
        {
            public const string DateFormat = "yyyy-MM-dd'T'HH:mm:ssZ";
        }

    }
}
