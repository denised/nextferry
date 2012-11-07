using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace WCFServiceWebRole1
{
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        string GetSchedules(string clientversion, string lastdataversion);

        [OperationContract]
        string GetDelays(string clientversion, string location);

        [OperationContract]
        string GetAlerts(string clientversion);
    }
}
