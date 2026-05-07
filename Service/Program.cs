using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(DroneTelemetryService));

            host.Open();

            Console.WriteLine("Drone telemetry service is running...");
            Console.WriteLine("Address: net.tcp://localhost:4002/DroneTelemetryService");
            Console.WriteLine("Press ENTER to stop service.");

            Console.ReadLine();

            host.Close();
        }
    }
}
