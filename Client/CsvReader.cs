using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Common;

namespace Client
{
    public class CsvReader : IDisposable
    {
        private StreamReader _reader;
        private bool _disposed = false;

        public CsvReader(string filePath)
        {
            _reader = new StreamReader(filePath);
        }

        public IEnumerable<ChargingData> ReadRows(string vehicleId, List<string> errorLog)
        {
            string header = _reader.ReadLine();
            int rowIndex = 0;

            while (!_reader.EndOfStream)
            {
                string line = _reader.ReadLine();
                rowIndex++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                ChargingData data = TryParseLine(line, vehicleId, rowIndex);

                if (data == null)
                {
                    errorLog.Add(string.Format("Row {0}: failed to parse -> {1}", rowIndex, line));
                    continue;
                }

                yield return data;
            }
        }

        private ChargingData TryParseLine(string line, string vehicleId, int rowIndex)
        {
            try
            {
                string[] cols = line.Split(',');

                return new ChargingData
                {
                    Timestamp      = DateTime.Parse(cols[0].Trim(), CultureInfo.InvariantCulture),
                    VoltageMin     = double.Parse(cols[1].Trim(), CultureInfo.InvariantCulture),
                    VoltageAvg     = double.Parse(cols[2].Trim(), CultureInfo.InvariantCulture),
                    VoltageMax     = double.Parse(cols[3].Trim(), CultureInfo.InvariantCulture),
                    CurrentMin     = double.Parse(cols[4].Trim(), CultureInfo.InvariantCulture),
                    CurrentAvg     = double.Parse(cols[5].Trim(), CultureInfo.InvariantCulture),
                    CurrentMax     = double.Parse(cols[6].Trim(), CultureInfo.InvariantCulture),
                    RealPowerMin   = double.Parse(cols[7].Trim(), CultureInfo.InvariantCulture),
                    RealPowerAvg   = double.Parse(cols[8].Trim(), CultureInfo.InvariantCulture),
                    RealPowerMax   = double.Parse(cols[9].Trim(), CultureInfo.InvariantCulture),
                    ReactivePowerMin  = double.Parse(cols[10].Trim(), CultureInfo.InvariantCulture),
                    ReactivePowerAvg  = double.Parse(cols[11].Trim(), CultureInfo.InvariantCulture),
                    ReactivePowerMax  = double.Parse(cols[12].Trim(), CultureInfo.InvariantCulture),
                    ApparentPowerMin  = double.Parse(cols[13].Trim(), CultureInfo.InvariantCulture),
                    ApparentPowerAvg  = double.Parse(cols[14].Trim(), CultureInfo.InvariantCulture),
                    ApparentPowerMax  = double.Parse(cols[15].Trim(), CultureInfo.InvariantCulture),
                    FrequencyMin   = double.Parse(cols[16].Trim(), CultureInfo.InvariantCulture),
                    FrequencyAvg   = double.Parse(cols[17].Trim(), CultureInfo.InvariantCulture),
                    FrequencyMax   = double.Parse(cols[18].Trim(), CultureInfo.InvariantCulture),
                    RowIndex       = rowIndex,
                    VehicleId      = vehicleId
                };
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_reader != null)
                    {
                        _reader.Close();
                        _reader = null;
                    }
                }
                _disposed = true;
            }
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}
