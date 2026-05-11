using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class DroneTelemetryService : IDroneTelemetryService
    {
        private static bool sessionStarted = false;
        private static int acceptedCount = 0;
        private static int rejectedCount = 0;
        public ServiceResponse StartSession(SessionMeta meta)
        {
            ValidateMeta(meta);

            sessionStarted = true;
            acceptedCount = 0;
            rejectedCount = 0;

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
                rejectedCount++;

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        FieldName = "Session",
                        Reason = "Session has not been started."
                    },
                    new FaultReason("Session has not been started."));
            }

            ValidateSample(sample);

            acceptedCount++;

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
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        FieldName = "Session",
                        Reason = "Cannot end session because session has not been started."
                    },
                    new FaultReason("Cannot end session because session has not been started."));
            }

            sessionStarted = false;

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

            DateTime parsedTime;

            if (!DateTime.TryParse(sample.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedTime))
            {
                ThrowDataFormatFault("Time", "Time format is not valid.");
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

        private void ThrowDataFormatFault(string fieldName, string reason)
        {
            rejectedCount++;

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
            rejectedCount++;

            throw new FaultException<ValidationFault>(
                new ValidationFault
                {
                    FieldName = fieldName,
                    Reason = reason
                },
                new FaultReason(reason));
        }
    }
}
