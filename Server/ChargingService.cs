using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using Common;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ChargingService : IChargingService, IDisposable
    {
        private FileStream _sessionFileStream;
        private StreamWriter _sessionWriter;
        private FileStream _rejectsFileStream;
        private StreamWriter _rejectsWriter;
        private bool _disposed = false;

        public void StartSession(string vehicleId)
        {
            CloseFileResources();

            string dataPath = ConfigurationManager.AppSettings["DataPath"] ?? "Data";
            string sessionDir = Path.Combine(dataPath, vehicleId, DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(sessionDir);

            string sessionFile = Path.Combine(sessionDir, "session.csv");
            string rejectsFile = Path.Combine(sessionDir, "rejects.csv");

            bool sessionIsNew = !File.Exists(sessionFile) || new FileInfo(sessionFile).Length == 0;
            bool rejectsIsNew = !File.Exists(rejectsFile) || new FileInfo(rejectsFile).Length == 0;

            _sessionFileStream = new FileStream(sessionFile, FileMode.Append, FileAccess.Write);
            _sessionWriter = new StreamWriter(_sessionFileStream);
            if (sessionIsNew)
                _sessionWriter.WriteLine(
                    "Timestamp,VoltageMin,VoltageAvg,VoltageMax," +
                    "CurrentMin,CurrentAvg,CurrentMax," +
                    "RealPowerMin,RealPowerAvg,RealPowerMax," +
                    "ReactivePowerMin,ReactivePowerAvg,ReactivePowerMax," +
                    "ApparentPowerMin,ApparentPowerAvg,ApparentPowerMax," +
                    "FrequencyMin,FrequencyAvg,FrequencyMax," +
                    "RowIndex,VehicleId");

            _rejectsFileStream = new FileStream(rejectsFile, FileMode.Append, FileAccess.Write);
            _rejectsWriter = new StreamWriter(_rejectsFileStream);
            if (rejectsIsNew)
                _rejectsWriter.WriteLine("RowIndex,VehicleId,Reason");

            Console.WriteLine(string.Format("[{0}] Sesija pokrenuta - Vozilo: {1} -> {2}",
                DateTime.Now, vehicleId, sessionDir));
        }

        public void PushSample(ChargingData data)
        {
            string error = GetValidationError(data);
            if (error != null)
            {
                WriteReject(data, error);
                ThrowFault(error);
            }

            WriteSessionRow(data);
            Console.WriteLine(string.Format("[prenos u toku] Red {0} primljen - Vozilo: {1}",
                data.RowIndex, data.VehicleId));
        }

        public void EndSession(string vehicleId)
        {
            CloseFileResources();
            Console.WriteLine(string.Format("[prenos završen] Sesija završena za vozilo: {0}", vehicleId));
        }

        private string GetValidationError(ChargingData data)
        {
            if (data.Timestamp == DateTime.MinValue)
                return "Invalid Timestamp.";
            if (data.VoltageAvg <= 0)
                return string.Format("Invalid VoltageAvg: {0}", data.VoltageAvg);
            if (data.FrequencyAvg <= 0)
                return string.Format("Invalid FrequencyAvg: {0}", data.FrequencyAvg);
            return null;
        }

        private void WriteSessionRow(ChargingData d)
        {
            if (_sessionWriter == null) return;
            _sessionWriter.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}",
                d.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                d.VoltageMin, d.VoltageAvg, d.VoltageMax,
                d.CurrentMin, d.CurrentAvg, d.CurrentMax,
                d.RealPowerMin, d.RealPowerAvg, d.RealPowerMax,
                d.ReactivePowerMin, d.ReactivePowerAvg, d.ReactivePowerMax,
                d.ApparentPowerMin, d.ApparentPowerAvg, d.ApparentPowerMax,
                d.FrequencyMin, d.FrequencyAvg, d.FrequencyMax,
                d.RowIndex, d.VehicleId));
            _sessionWriter.Flush();
        }

        private void WriteReject(ChargingData data, string reason)
        {
            if (_rejectsWriter == null) return;
            _rejectsWriter.WriteLine(string.Format("{0},{1},{2}", data.RowIndex, data.VehicleId, reason));
            _rejectsWriter.Flush();
        }

        private void ThrowFault(string message)
        {
            throw new FaultException<ChargingFault>(
                new ChargingFault(message),
                new FaultReason(message));
        }

        protected void CloseFileResources()
        {
            if (_sessionWriter != null)
            {
                _sessionWriter.Flush();
                _sessionWriter.Close();
                _sessionWriter = null;
            }
            if (_sessionFileStream != null)
            {
                _sessionFileStream.Close();
                _sessionFileStream = null;
            }
            if (_rejectsWriter != null)
            {
                _rejectsWriter.Flush();
                _rejectsWriter.Close();
                _rejectsWriter = null;
            }
            if (_rejectsFileStream != null)
            {
                _rejectsFileStream.Close();
                _rejectsFileStream = null;
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
                    CloseFileResources();
                _disposed = true;
            }
        }

        ~ChargingService()
        {
            Dispose(false);
        }
    }
}
