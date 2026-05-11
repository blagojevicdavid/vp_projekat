using System;
using System.IO;
using System.ServiceModel;
using Common;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ChargingService : IChargingService, IDisposable
    {
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        private bool _disposed = false;

        public void StartSession(string vehicleId)
        {
            Console.WriteLine(string.Format("[{0}] Session started for vehicle: {1}", DateTime.Now, vehicleId));
        }

        public void PushSample(ChargingData data)
        {
            ValidateSample(data);
            Console.WriteLine(string.Format("[{0}] Sample received - Vehicle: {1}, Row: {2}", DateTime.Now, data.VehicleId, data.RowIndex));
        }

        public void EndSession(string vehicleId)
        {
            Console.WriteLine(string.Format("[{0}] Session ended for vehicle: {1}", DateTime.Now, vehicleId));
            CloseFileResources();
        }

        private void ValidateSample(ChargingData data)
        {
            if (data.Timestamp == DateTime.MinValue)
                ThrowFault("Invalid Timestamp.");

            if (data.VoltageAvg <= 0)
                ThrowFault(string.Format("Invalid VoltageAvg: {0}", data.VoltageAvg));

            if (data.FrequencyAvg <= 0)
                ThrowFault(string.Format("Invalid FrequencyAvg: {0}", data.FrequencyAvg));
        }

        private void ThrowFault(string message)
        {
            throw new FaultException<ChargingFault>(
                new ChargingFault(message),
                new FaultReason(message));
        }

        protected void OpenFileResources(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            _streamWriter = new StreamWriter(_fileStream);
        }

        protected void CloseFileResources()
        {
            if (_streamWriter != null)
            {
                _streamWriter.Flush();
                _streamWriter.Close();
                _streamWriter = null;
            }
            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream = null;
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
                    CloseFileResources();
                }
                _disposed = true;
            }
        }

        ~ChargingService()
        {
            Dispose(false);
        }
    }
}
