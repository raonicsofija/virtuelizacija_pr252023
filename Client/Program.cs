using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ChannelFactory<IDroneTelemetryService> factory =
                    new ChannelFactory<IDroneTelemetryService>("DroneTelemetryService");

                IDroneTelemetryService proxy = factory.CreateChannel();

                SessionMeta meta = new SessionMeta
                {
                    Header = new string[]
                    {
                        "LinearAccelerationX",
                        "LinearAccelerationY",
                        "LinearAccelerationZ",
                        "WindSpeed",
                        "WindAngle",
                        "Time"
                    }
                };

                Console.WriteLine(proxy.StartSession(meta));

                DroneSample sample = new DroneSample
                {
                    LinearAccelerationX = 0.12,
                    LinearAccelerationY = 0.05,
                    LinearAccelerationZ = 9.81,
                    WindSpeed = 4.5,
                    WindAngle = 120,
                    Time = DateTime.Now.ToString()
                };

                Console.WriteLine(proxy.PushSample(sample));
                Console.WriteLine(proxy.EndSession());

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client error:");
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }
    }
}
