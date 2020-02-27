namespace Test.Activities.Automation.ActivityLib.Utils.Constants
{
    public static class Constants
    {
        public const string ServiceUrl = @"http://localhost/projects2/_vti_bin/AutomationService.svc/FillActivities";
        public const string Host = "http://localhost";
        public const string Web = "Projects2";

        public static class Activity
        {
            public const string Year = "Year";
            public const string Month = "Month";
            public const string Mentor = "Mentor";
            public const string RootMentor = "Root Mentor";
            public const string Activities = "Activities";
            public const string Employee = "Employee";
            public const string Paths = "Paths";
            public const string Date = "Date";

            public static class ActivityType
            {
                public const string Mentoring = "Mentoring";
                public const string RootMentoring = "Root Mentoring";
            }
        }

        public static class Lists
        {
            public const string MentoringCalendar = "Timetable";
            public const string Configurations = "Configuration";
            public const string Activities = "Activities";
            public const string Mentors = "Mentors";
            public const string RootMentors = "Root Mentors";
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
            public const string StartTime = "EventDate";
            public const string EndTime = "EndDate";
            public const string Mentor = "Mentor";
            public const string RootMentor = "Root Mentor";
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
