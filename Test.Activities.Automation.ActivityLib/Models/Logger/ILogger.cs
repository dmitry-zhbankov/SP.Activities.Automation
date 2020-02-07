namespace Test.Activities.Automation.ActivityLib.Models
{
    public interface ILogger
    {
        void LogError(string message);

        void LogWarning(string message);

        void LogInformation(string message);
    }
}
