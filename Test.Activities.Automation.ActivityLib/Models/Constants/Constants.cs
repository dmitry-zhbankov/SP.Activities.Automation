namespace Test.Activities.Automation.ActivityLib.Models
{
    public static class Constants
    {
        public const string ServiceUrl = @"http://localhost/_vti_bin/AutomationService.svc/FillActivities";
        public const string Host = "http://localhost";
        public const string Web = "Projects2";

        public static class Activity
        {
            public const string Year = "Year";
            public const string Month = "Month";
            public const string User = "User";
            public const string Activities = "Activities";

            public static class ActivityType
            {
                public const string Mentoring = "Mentoring";
            }
        }

        public static class Lists
        {
            public const string MentoringCalendar = "MentoringCalendar";
            public const string Configurations = "Configuration";
            public const string Activities = "Activities";
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
            public const string Since = "since=";
            public const string Until = "until=";
            public const string Branch = "ref_name=";
            public const string Commits = "repository/commits";
            public static readonly string Token = Properties.Resources.Token;
            public const string PrivateToken = "Private-Token";
            public const string Branches = "repository/branches";
        }

        public static class Calendar
        {
            public const string StartTime = "Start Time";
            public const string EndTime = "End Time";
            public const string Employee = "Employee";
        }

        public static class HttpHeader
        {
            public static class MediaType
            {
                public const string ApplicationJson = "application/json";
            }
        }
    }
}
