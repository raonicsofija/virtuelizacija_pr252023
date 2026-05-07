using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class DroneTelemetryService : IDroneTelemetryService
    {
        public string StartSession(SessionMeta meta)
        {
            return "Session started.";
        }
        public string PushSample(DroneSample sample)
        {
            return "Sample received.";
        }
        public string EndSession()
        {
            return "Session ended.";
        }
    }
}
