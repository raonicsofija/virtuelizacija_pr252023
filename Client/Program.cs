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
            ChannelFactory<IDroneTelemetryService> factory = null;
            IDroneTelemetryService proxy = null;

            try
            {
                factory = new ChannelFactory<IDroneTelemetryService>("DroneTelemetryService");

                proxy = factory.CreateChannel();

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

                ServiceResponse startResponse = proxy.StartSession(meta);
                PrintResponse(startResponse);

                DroneSample sample = new DroneSample
                {
                    LinearAccelerationX = 0.12,
                    LinearAccelerationY = 0.05,
                    LinearAccelerationZ = 9.81,
                    WindSpeed = 4.5,
                    WindAngle = 120,
                    Time = DateTime.Now.ToString()
                };

                ServiceResponse pushResponse = proxy.PushSample(sample);
                PrintResponse(pushResponse);

                ServiceResponse endResponse = proxy.EndSession();
                PrintResponse(endResponse);

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("DATA FORMAT FAULT");
                Console.WriteLine("Field: " + ex.Detail.FieldName);
                Console.WriteLine("Reason: " + ex.Detail.Reason);

                AbortChannel(proxy, factory);
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("VALIDATION FAULT");
                Console.WriteLine("Field: " + ex.Detail.FieldName);
                Console.WriteLine("Reason: " + ex.Detail.Reason);

                AbortChannel(proxy, factory);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CLIENT ERROR");
                Console.WriteLine(ex.Message);

                AbortChannel(proxy, factory);
            }

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        private static void PrintResponse(ServiceResponse response)
        {
            Console.WriteLine("ACK: " + response.Ack);
            Console.WriteLine("Message: " + response.Message);
            Console.WriteLine("Status: " + response.Status);
            Console.WriteLine("Accepted: " + response.AcceptedCount);
            Console.WriteLine("Rejected: " + response.RejectedCount);
            Console.WriteLine();
        }

        private static void AbortChannel(IDroneTelemetryService proxy, ChannelFactory<IDroneTelemetryService> factory)
        {
            IClientChannel clientChannel = proxy as IClientChannel;

            if (clientChannel != null)
            {
                clientChannel.Abort();
            }

            if (factory != null)
            {
                factory.Abort();
            }
        }
    }
}
