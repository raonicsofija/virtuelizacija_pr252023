using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface IDroneTelemetryService
    {
        [OperationContract]
        string StartSession(SessionMeta meta);

        [OperationContract]
        string PushSample(DroneSample sample);

        [OperationContract]
        string EndSession();
    }
}
