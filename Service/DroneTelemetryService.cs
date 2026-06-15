using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace Service
{
    public class DroneTelemetryService : IDroneTelemetryService
    {
        private static bool sessionStarted = false;
        private static int acceptedCount = 0;
        private static int rejectedCount = 0;
        private static TextFileManager fileManager = null;
        private static TextFileManager measurementsFileManager = null;
        private static TextFileManager rejectsFileManager = null;
        public ServiceResponse StartSession(SessionMeta meta)
        {
            ValidateMeta(meta);

            DisposeFileManagers();

            string storagePath = GetStoragePath();

            fileManager = new TextFileManager(Path.Combine(storagePath, "transfer_log.txt"));
            measurementsFileManager = new TextFileManager(Path.Combine(storagePath, "measurements_session.csv"));
            rejectsFileManager = new TextFileManager(Path.Combine(storagePath, "rejects.csv"));

            fileManager.ClearText();

            measurementsFileManager.ClearAndWriteLine("LinearAccelerationX,LinearAccelerationY,LinearAccelerationZ,WindSpeed,WindAngle,Time");
            rejectsFileManager.ClearAndWriteLine("Reason,LinearAccelerationX,LinearAccelerationY,LinearAccelerationZ,WindSpeed,WindAngle,Time");

            sessionStarted = true;
            acceptedCount = 0;
            rejectedCount = 0;

            Console.WriteLine("Transfer in progress...");

            fileManager.AppendText("StartSession called.");
            fileManager.AppendText("Session started.");

            return new ServiceResponse
            {
                Ack = true,
                Message = "Session started.",
                Status = TransferStatus.IN_PROGRESS,
                AcceptedCount = acceptedCount,
                RejectedCount = rejectedCount
            };
        }
        public ServiceResponse PushSample(DroneSample sample)
        {
            if (!sessionStarted)
            {
                ThrowValidationFault("Session", "Session has not been started.");
            }

            ValidateSample(sample);

            acceptedCount++;

            Console.WriteLine("Transfer in progress... received sample number " + acceptedCount);

            if (measurementsFileManager != null)
            {
                measurementsFileManager.AppendLine(SampleToCsv(sample));
            }

            if (fileManager != null)
            {
                fileManager.AppendText("Sample accepted. Accepted count = " + acceptedCount);
            }

            return new ServiceResponse
            {
                Ack = true,
                Message = "Sample accepted.",
                Status = TransferStatus.IN_PROGRESS,
                AcceptedCount = acceptedCount,
                RejectedCount = rejectedCount
            };
        }
        public ServiceResponse EndSession()
        {
            if (!sessionStarted)
            {
                ThrowValidationFault("Session", "Cannot end session because session has not been started.");
            }

            sessionStarted = false;

            Console.WriteLine("Transfer completed.");

            if (fileManager != null)
            {
                fileManager.AppendText("EndSession called.");
                fileManager.AppendText("Transfer completed. Accepted: " + acceptedCount + ", Rejected: " + rejectedCount);
                fileManager.AppendText("Dispose will be called now.");

                DisposeFileManagers();
            }

            return new ServiceResponse
            {
                Ack = true,
                Message = "Transfer completed.",
                Status = TransferStatus.COMPLETED,
                AcceptedCount = acceptedCount,
                RejectedCount = rejectedCount
            };
        }

        private void ValidateMeta(SessionMeta meta)
        {
            if (meta == null)
            {
                ThrowDataFormatFault("Meta", "Meta header is missing.");
            }

            if (meta.Header == null || meta.Header.Length == 0)
            {
                ThrowDataFormatFault("Header", "Header is missing.");
            }

            string[] requiredColumns =
            {
                "LinearAccelerationX",
                "LinearAccelerationY",
                "LinearAccelerationZ",
                "WindSpeed",
                "WindAngle",
                "Time"
            };

            foreach (string requiredColumn in requiredColumns)
            {
                bool exists = false;

                foreach (string column in meta.Header)
                {
                    if (string.Equals(column.Trim(), requiredColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    ThrowDataFormatFault(requiredColumn, "Required column is missing in header.");
                }
            }
        }

        private void ValidateSample(DroneSample sample)
        {
            if (sample == null)
            {
                ThrowDataFormatFault("Sample", "Sample is null.");
            }

            if (string.IsNullOrWhiteSpace(sample.Time))
            {
                ThrowValidationFault("Time", "Time is required.");
            }

            double parsedTime;

            if (!double.TryParse(sample.Time, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedTime))
            {
                ThrowDataFormatFault("Time", "Time format is not valid.");
            }

            if (parsedTime < 0)
            {
                ThrowValidationFault("Time", "Time must be greater than or equal to 0.");
            }

            if (double.IsNaN(sample.LinearAccelerationX) || double.IsInfinity(sample.LinearAccelerationX))
            {
                ThrowDataFormatFault("LinearAccelerationX", "Invalid numeric value.");
            }

            if (double.IsNaN(sample.LinearAccelerationY) || double.IsInfinity(sample.LinearAccelerationY))
            {
                ThrowDataFormatFault("LinearAccelerationY", "Invalid numeric value.");
            }

            if (double.IsNaN(sample.LinearAccelerationZ) || double.IsInfinity(sample.LinearAccelerationZ))
            {
                ThrowDataFormatFault("LinearAccelerationZ", "Invalid numeric value.");
            }

            if (double.IsNaN(sample.WindSpeed) || double.IsInfinity(sample.WindSpeed))
            {
                ThrowDataFormatFault("WindSpeed", "Invalid numeric value.");
            }

            if (double.IsNaN(sample.WindAngle) || double.IsInfinity(sample.WindAngle))
            {
                ThrowDataFormatFault("WindAngle", "Invalid numeric value.");
            }

            if (sample.WindSpeed <= 0)
            {
                ThrowValidationFault("WindSpeed", "WindSpeed must be greater than 0.");
            }

            if (sample.WindAngle < 0 || sample.WindAngle > 360)
            {
                ThrowValidationFault("WindAngle", "WindAngle must be between 0 and 360 degrees.");
            }

            double accelerationNorm = Math.Sqrt(
                sample.LinearAccelerationX * sample.LinearAccelerationX +
                sample.LinearAccelerationY * sample.LinearAccelerationY +
                sample.LinearAccelerationZ * sample.LinearAccelerationZ);

            if (accelerationNorm == 0)
            {
                ThrowValidationFault("AccelerationNorm", "Acceleration norm cannot be zero.");
            }
        }

        private void CloseFileManagerAfterError(string reason)
        {
            if (fileManager != null)
            {
                fileManager.AppendText("Transfer interrupted. Reason: " + reason);
                fileManager.AppendText("Dispose will be called after error.");

                DisposeFileManagers();
            }

            sessionStarted = false;
        }

        private void ThrowDataFormatFault(string fieldName, string reason)
        {
            WriteReject(reason);

            CloseFileManagerAfterError(reason);

            throw new FaultException<DataFormatFault>(
                new DataFormatFault
                {
                    FieldName = fieldName,
                    Reason = reason
                },
                new FaultReason(reason));
        }

        private void ThrowValidationFault(string fieldName, string reason)
        {
            WriteReject(reason);

            CloseFileManagerAfterError(reason);

            throw new FaultException<ValidationFault>(
                new ValidationFault
                {
                    FieldName = fieldName,
                    Reason = reason
                },
                new FaultReason(reason));
        }

        private string GetStoragePath()
        {
            string storagePath = ConfigurationManager.AppSettings["StoragePath"];

            if (string.IsNullOrWhiteSpace(storagePath))
            {
                storagePath = "ServiceStorage";
            }

            return storagePath;
        }

        private string SampleToCsv(DroneSample sample)
        {
            return sample.LinearAccelerationX.ToString(CultureInfo.InvariantCulture) + "," +
                   sample.LinearAccelerationY.ToString(CultureInfo.InvariantCulture) + "," +
                   sample.LinearAccelerationZ.ToString(CultureInfo.InvariantCulture) + "," +
                   sample.WindSpeed.ToString(CultureInfo.InvariantCulture) + "," +
                   sample.WindAngle.ToString(CultureInfo.InvariantCulture) + "," +
                   sample.Time;
        }

        private void WriteReject(string reason)
        {
            rejectedCount++;

            if (rejectsFileManager != null)
            {
                rejectsFileManager.AppendLine(reason + ",,,,,,");
            }
        }

        private void DisposeFileManagers()
        {
            if (fileManager != null)
            {
                fileManager.Dispose();
                fileManager = null;
            }

            if (measurementsFileManager != null)
            {
                measurementsFileManager.Dispose();
                measurementsFileManager = null;
            }

            if (rejectsFileManager != null)
            {
                rejectsFileManager.Dispose();
                rejectsFileManager = null;
            }
        }
    }
}
