using Common;
using System;
using System.Collections.Generic;
using System.IO;
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

            string csvFileName;

            do
            {
                Console.WriteLine("Enter CSV file name:");
                csvFileName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(csvFileName))
                {
                    Console.WriteLine("CSV file name is required.");
                }

            } while (string.IsNullOrWhiteSpace(csvFileName));

            string csvPath = Path.Combine("..", "..", "..", "Data", csvFileName);

            try
            {
                using (CsvDroneReader reader = new CsvDroneReader(csvPath))
                {
                    List<DroneSample> samples = reader.ReadFirst120Rows();

                    Console.WriteLine("Loaded samples: " + samples.Count);

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

                    for (int i = 0; i < samples.Count; i++)
                    {
                        ServiceResponse pushResponse = proxy.PushSample(samples[i]);

                        Console.WriteLine("Sample " + (i + 1) + " sent.");
                        PrintResponse(pushResponse);
                    }

                    ServiceResponse endResponse = proxy.EndSession();
                    PrintResponse(endResponse);

                    ((IClientChannel)proxy).Close();
                    factory.Close();
                }
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
