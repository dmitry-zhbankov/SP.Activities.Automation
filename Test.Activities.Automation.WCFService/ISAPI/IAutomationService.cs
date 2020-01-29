using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace Test.Activities.Automation.WCFService
{
    [ServiceContract]
    interface IAutomationService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "FillActivities",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare
            )]
        bool FillActivities(object[] activities);
    }
}
