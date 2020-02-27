using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Activities.Automation.ActivityLib.Models;

namespace Test.Activities.Automation.Test
{
    class DebugLogger : ILogger
    {
        public void LogError(string message)
        {
            Debug.WriteLine($"Error: {message}");
        }

        public void LogWarning(string message)
        {
            Debug.WriteLine($"Warning: {message}");
        }

        public void LogInformation(string message)
        {
            Debug.WriteLine($"Information: {message}");
        }
    }
}
