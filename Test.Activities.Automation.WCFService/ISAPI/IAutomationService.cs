using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using Test.Activities.Automation.ActivityLib;
using Test.Activities.Automation.ActivityLib.Models;

namespace Test.Activities.Automation.WCFService
{
    [ServiceContract]
    interface IAutomationService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "FillActivities",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        HttpStatusCode FillActivities(IEnumerable<InfoActivity> activities);
    }
}
