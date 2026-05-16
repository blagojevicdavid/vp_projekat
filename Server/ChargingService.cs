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
        // --- Dogadjaji (Zadatak 8) ---
        public event TransferStartedHandler OnTransferStarted;
        public event SampleReceivedHandler OnSampleReceived;
        public event TransferCompletedHandler OnTransferCompleted;
        public event WarningRaisedHandler OnWarningRaised;

        // --- Stanje fajlova ---
        private FileStream _sessionFileStream;
        private StreamWriter _sessionWriter;
        private FileStream _rejectsFileStream;
        private StreamWriter _rejectsWriter;
        private bool _disposed = false;

        // --- Stanje analitike (Zadaci 9 i 10) ---
        private double _prevCurrentAvg = double.NaN;
        private double _currentMean = 0;
        private int _currentCount = 0;
        private double _prevApparentPowerAvg = double.NaN;
        private int _apparentPowerStallCount = 0;

        public void StartSession(string vehicleId)
        {
            CloseFileResources();
            ResetAnalyticsState();

            string dataPath = ConfigurationManager.AppSettings["DataPath"];
            if (dataPath == null)
                dataPath = "Data";
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

            if (OnTransferStarted != null)
                OnTransferStarted(this, new TransferStartedEventArgs(vehicleId));
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public void PushSample(SampleOptions options)
        {
            if (options == null || options.Data == null || options.Data.Length == 0)
                ThrowFault("Empty sample payload.");

            ChargingData data;
            try
            {
                data = ChargingData.FromBytes(options.Data);
            }
            catch (Exception ex)
            {
                ThrowFault("Failed to deserialize sample: " + ex.Message);
                return;
            }

            string error = GetValidationError(data);
            if (error != null)
            {
                WriteReject(data, error);
                ThrowFault(error);
            }

            WriteSessionRow(data);
            RunAnalytics(data);
            if (OnSampleReceived != null)
                OnSampleReceived(this, new SampleReceivedEventArgs(data.VehicleId, data.RowIndex));
        }

        public void EndSession(string vehicleId)
        {
            CloseFileResources();
            if (OnTransferCompleted != null)
                OnTransferCompleted(this, new TransferCompletedEventArgs(vehicleId));
        }

        // --- Analitika ---

        private void ResetAnalyticsState()
        {
            _prevCurrentAvg = double.NaN;
            _currentMean = 0;
            _currentCount = 0;
            _prevApparentPowerAvg = double.NaN;
            _apparentPowerStallCount = 0;
        }

        private void RunAnalytics(ChargingData data)
        {
            double spikeThreshold = ParseConfig("CurrentSpikeThreshold", 5.0);
            double reactivePowerThreshold = ParseConfig("ReactivePowerThreshold", 100.0);

            // Zadatak 9: CurrentSpike - delta izmedju uzastopnih merenja
            if (!double.IsNaN(_prevCurrentAvg))
            {
                double delta = data.CurrentAvg - _prevCurrentAvg;
                if (Math.Abs(delta) > spikeThreshold)
                {
                    string direction = delta > 0 ? "porast" : "pad";
                    RaiseWarning(data, "CurrentSpike", _prevCurrentAvg, data.CurrentAvg,
                        string.Format(CultureInfo.InvariantCulture,
                            "dI={0:F4} ({1})", delta, direction));
                }
            }
            _prevCurrentAvg = data.CurrentAvg;

            // Zadatak 9: CurrentOutOfBand - odstupanje +-20% od tekuceg proseka
            if (_currentCount > 0)
            {
                double lower = 0.80 * _currentMean;
                double upper = 1.20 * _currentMean;
                if (data.CurrentAvg < lower || data.CurrentAvg > upper)
                {
                    RaiseWarning(data, "CurrentOutOfBand", _currentMean, data.CurrentAvg,
                        string.Format(CultureInfo.InvariantCulture,
                            "van opsega [{0:F4}, {1:F4}]", lower, upper));
                }
            }
            _currentCount++;
            _currentMean += (data.CurrentAvg - _currentMean) / _currentCount;

            // Zadatak 10: ReactivePowerWarning - prelaz praga
            if (data.ReactivePowerAvg > reactivePowerThreshold)
            {
                RaiseWarning(data, "ReactivePowerWarning", reactivePowerThreshold, data.ReactivePowerAvg,
                    string.Format(CultureInfo.InvariantCulture,
                        "ReactivePowerAvg={0:F4} > prag {1:F4}", data.ReactivePowerAvg, reactivePowerThreshold));
            }

            // Zadatak 10: ApparentPowerStall - stagnacija prividne snage (>=5 uzastopnih redova)
            if (!double.IsNaN(_prevApparentPowerAvg))
            {
                if (data.ApparentPowerAvg <= _prevApparentPowerAvg)
                {
                    _apparentPowerStallCount++;
                    if (_apparentPowerStallCount >= 5)
                    {
                        RaiseWarning(data, "ApparentPowerStall", _prevApparentPowerAvg, data.ApparentPowerAvg,
                            string.Format("prividna snaga ne raste vec {0} redova", _apparentPowerStallCount));
                    }
                }
                else
                {
                    _apparentPowerStallCount = 0;
                }
            }
            _prevApparentPowerAvg = data.ApparentPowerAvg;
        }

        private void RaiseWarning(ChargingData data, string warningType,
            double valueBefore, double valueAfter, string message)
        {
            if (OnWarningRaised != null)
                OnWarningRaised(this, new WarningEventArgs(
                    data.VehicleId, data.RowIndex, warningType, valueBefore, valueAfter, message));
        }

        private double ParseConfig(string key, double defaultValue)
        {
            string raw = ConfigurationManager.AppSettings[key];
            double result;
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                ? result
                : defaultValue;
        }

        // --- Validacija i fajlovi ---

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
