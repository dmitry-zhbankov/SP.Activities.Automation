using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Test.Activities.Automation.ActivityLib;
using Test.Activities.Automation.TimerJob.Constants;

namespace Test.Activities.Automation.TimerJob
{
    public class TimerJob : SPJobDefinition
    {
        public TimerJob() : base()
        {
        }

        public TimerJob(string name, SPWebApplication webApp, SPServer server, SPJobLockType lockType) : base(name, webApp, server, lockType)
        {
        }

        public override void Execute(Guid targetInstanceId)
        {
            var serializer = new DataContractJsonSerializer(typeof(ActivityInfo[]));
            var ms = new MemoryStream();
            serializer.WriteObject(ms, new[]
            {
                new ActivityInfo
                {
                    UserId = 1,
                    Activity = "Development",
                    Date = DateTime.Now
                },
                new ActivityInfo
                {
                    UserId = 2,
                    Activity = "Mentoring",
                    Date = DateTime.Now
                },
            });

            ms.Position = 0;
            var reader = new StreamReader(ms);
            var str = reader.ReadToEnd();

            var handler = new HttpClientHandler()
            {
                UseDefaultCredentials = true,
            };
            var client = new HttpClient(handler);
            
            var content=new StringContent(str,Encoding.UTF8,@"application/json");
            var uri=new Uri(TimerJobConstants.ServiceUrl);

            var res = client.PostAsync(uri, content).Result;
        }
    }
}
