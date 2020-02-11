using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Test.Activities.Automation.ActivityLib;
using Test.Activities.Automation.ActivityLib.Models;
using Test.Activities.Automation.ActivityLib.Utils.Constants;

namespace Test.Activities.Automation.TimerJob
{
    public class TimerJob : SPJobDefinition
    {
        private const int RequestAttempts = 3;

        private ILogger _logger;

        public TimerJob()
        {
            try
            {
                _logger = new ULSLogger(GetType().FullName);
            }
            catch
            {
            }
        }

        public TimerJob(string name, SPWebApplication webApp, SPServer server, SPJobLockType lockType)
            : base(name, webApp, server, lockType)
        {
        }

        public override void Execute(Guid targetInstanceId)
        {
            try
            {
                _logger?.LogInformation("Timer executing");

                try
                {
                    var activityService=new FetchActivityService(_logger);

                    var activities = activityService.GetActivities();

                    SendActivities(activities);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.Message);
                }

                _logger?.LogInformation("Timer has executed");
            }
            catch
            {
            }
        }

        private async void SendActivities(IEnumerable<InfoActivity> activities)
        {
            try
            {
                _logger?.LogInformation("Sending activities to service");

                var serializer = new DataContractJsonSerializer(typeof(List<InfoActivity>));

                var ms = new MemoryStream();
                serializer.WriteObject(ms, activities);

                ms.Position = 0;
                var reader = new StreamReader(ms);
                var str = reader.ReadToEnd();

                using (var svcHandler = new HttpClientHandler { UseDefaultCredentials = true })
                using (var svcClient = new HttpClient(svcHandler))
                {
                    var content = new StringContent(str, Encoding.UTF8, Constants.HttpHeader.MediaType.ApplicationJson);
                    var uri = new Uri(Constants.ServiceUrl);

                    for (var i = 0; i < RequestAttempts; i++)
                    {
                        var res = await svcClient.PostAsync(uri, content);
                        if (res.StatusCode == HttpStatusCode.OK) return;

                        _logger?.LogWarning($"Request {i} failed");
                        
                        if (i!=RequestAttempts)
                        {
                            await Task.Delay(new TimeSpan(0, 5, 0));
                        }
                    }

                    throw new Exception("All requests to service failed");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Sending activities to service failed. {e.Message}");
            }
        }
    }
}
