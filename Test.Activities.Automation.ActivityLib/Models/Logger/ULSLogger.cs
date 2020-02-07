using Microsoft.SharePoint.Administration;

namespace Test.Activities.Automation.ActivityLib.Models
{
    public class ULSLogger : ILogger
    {
        private SPDiagnosticsService _service;
        private string _category;

        public ULSLogger(string category)
        {
            _service = SPDiagnosticsService.Local;
            _category = category;
        }

        public void LogError(string message)
        {
            Log(TraceSeverity.Unexpected, EventSeverity.Error, message);
        }

        public void LogInformation(string message)
        {
            Log(TraceSeverity.Verbose, EventSeverity.Information, message);
        }

        public void LogWarning(string message)
        {
            Log(TraceSeverity.Medium, EventSeverity.Warning, message);
        }

        private void Log(TraceSeverity traceSeverity, EventSeverity eventSeverity, string message)
        {
            _service.WriteTrace(0, new SPDiagnosticsCategory(_category, traceSeverity, eventSeverity), traceSeverity, message);
        }
    }
}
