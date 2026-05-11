using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class CsvDroneReader : IDisposable
    {
        private readonly string csvPath;

        private StreamReader reader;
        private StreamWriter invalidRowsWriter;
        private StreamWriter extraRowsWriter;

        private Dictionary<string, int> columnIndexes;

        private bool disposed = false;

        public string[] Header { get; private set; }

        public CsvDroneReader(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                throw new ArgumentException("CSV path is empty.");
            }

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException("CSV file was not found.", csvPath);
            }

            this.csvPath = csvPath;

            Directory.CreateDirectory("ClientLogs");

            reader = new StreamReader(this.csvPath);
            invalidRowsWriter = new StreamWriter("ClientLogs\\invalid_rows.log", false);
            extraRowsWriter = new StreamWriter("ClientLogs\\extra_rows.log", false);
        }

        public List<DroneSample> ReadFirst120Rows()
        {
            CheckIfDisposed();

            List<DroneSample> samples = new List<DroneSample>(120);

            string headerLine = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new Exception("CSV file is empty.");
            }

            Header = SplitCsvLine(headerLine);
            columnIndexes = BuildColumnIndexes(Header);

            string line;
            int rowNumber = 1;

            while ((line = reader.ReadLine()) != null)
            {
                rowNumber++;

                try
                {
                    DroneSample sample = ParseSample(line);

                    if (samples.Count < 120)
                    {
                        samples.Add(sample);
                    }
                    else
                    {
                        extraRowsWriter.WriteLine("Row " + rowNumber + ": " + line);
                    }
                }
                catch (Exception ex)
                {
                    invalidRowsWriter.WriteLine("Row " + rowNumber + ": " + line);
                    invalidRowsWriter.WriteLine("Reason: " + ex.Message);
                    invalidRowsWriter.WriteLine();
                }
            }

            invalidRowsWriter.Flush();
            extraRowsWriter.Flush();

            return samples;
        }

        private Dictionary<string, int> BuildColumnIndexes(string[] header)
        {
            Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < header.Length; i++)
            {
                string columnName = header[i].Trim();

                if (!indexes.ContainsKey(columnName))
                {
                    indexes.Add(columnName, i);
                }
            }

            string[] requiredColumns =
            {
                "linear_acceleration_x",
                "linear_acceleration_y",
                "linear_acceleration_z",
                "wind_speed",
                "wind_angle",
                "time"
            };

            foreach (string requiredColumn in requiredColumns)
            {
                if (!indexes.ContainsKey(requiredColumn))
                {
                    throw new Exception("Required CSV column is missing: " + requiredColumn);
                }
            }

            return indexes;
        }

        private DroneSample ParseSample(string line)
        {
            string[] parts = SplitCsvLine(line);

            DroneSample sample = new DroneSample
            {
                LinearAccelerationX = ParseDouble(parts, "linear_acceleration_x"),
                LinearAccelerationY = ParseDouble(parts, "linear_acceleration_y"),
                LinearAccelerationZ = ParseDouble(parts, "linear_acceleration_z"),
                WindSpeed = ParseDouble(parts, "wind_speed"),
                WindAngle = ParseDouble(parts, "wind_angle"),
                Time = GetString(parts, "time")
            };

            return sample;
        }

        private string[] SplitCsvLine(string line)
        {
            return line.Split(',');
        }

        private double ParseDouble(string[] parts, string columnName)
        {
            string value = GetString(parts, columnName);

            return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private string GetString(string[] parts, string columnName)
        {
            int index = columnIndexes[columnName];

            if (index >= parts.Length)
            {
                throw new Exception("Column " + columnName + " does not exist in this row.");
            }

            string value = parts[index].Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception("Column " + columnName + " is empty.");
            }

            return value;
        }

        private void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("CsvDroneReader");
            }
        }

        ~CsvDroneReader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                        reader = null;
                    }

                    if (invalidRowsWriter != null)
                    {
                        invalidRowsWriter.Dispose();
                        invalidRowsWriter = null;
                    }

                    if (extraRowsWriter != null)
                    {
                        extraRowsWriter.Dispose();
                        extraRowsWriter = null;
                    }
                }

                disposed = true;
            }
        }
    }
}
