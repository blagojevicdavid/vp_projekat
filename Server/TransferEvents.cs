using System;

namespace Server
{
    public delegate void TransferStartedHandler(object sender, TransferStartedEventArgs e);
    public delegate void SampleReceivedHandler(object sender, SampleReceivedEventArgs e);
    public delegate void TransferCompletedHandler(object sender, TransferCompletedEventArgs e);
    public delegate void WarningRaisedHandler(object sender, WarningEventArgs e);

    public class TransferStartedEventArgs : EventArgs
    {
        public string VehicleId { get; }
        public DateTime Time { get; }

        public TransferStartedEventArgs(string vehicleId)
        {
            VehicleId = vehicleId;
            Time = DateTime.Now;
        }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public string VehicleId { get; }
        public int RowIndex { get; }

        public SampleReceivedEventArgs(string vehicleId, int rowIndex)
        {
            VehicleId = vehicleId;
            RowIndex = rowIndex;
        }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public string VehicleId { get; }
        public DateTime Time { get; }

        public TransferCompletedEventArgs(string vehicleId)
        {
            VehicleId = vehicleId;
            Time = DateTime.Now;
        }
    }

    public class WarningEventArgs : EventArgs
    {
        public string VehicleId { get; }
        public int RowIndex { get; }
        public string WarningType { get; }
        public double ValueBefore { get; }
        public double ValueAfter { get; }
        public string Message { get; }

        public WarningEventArgs(string vehicleId, int rowIndex, string warningType,
            double valueBefore, double valueAfter, string message)
        {
            VehicleId = vehicleId;
            RowIndex = rowIndex;
            WarningType = warningType;
            ValueBefore = valueBefore;
            ValueAfter = valueAfter;
            Message = message;
        }
    }
}
