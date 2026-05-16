using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Common
{
    [DataContract]
    public class ChargingData
    {
        [DataMember] public DateTime Timestamp { get; set; }

        [DataMember] public double VoltageMin { get; set; }
        [DataMember] public double VoltageAvg { get; set; }
        [DataMember] public double VoltageMax { get; set; }

        [DataMember] public double CurrentMin { get; set; }
        [DataMember] public double CurrentAvg { get; set; }
        [DataMember] public double CurrentMax { get; set; }

        [DataMember] public double RealPowerMin { get; set; }
        [DataMember] public double RealPowerAvg { get; set; }
        [DataMember] public double RealPowerMax { get; set; }

        [DataMember] public double ReactivePowerMin { get; set; }
        [DataMember] public double ReactivePowerAvg { get; set; }
        [DataMember] public double ReactivePowerMax { get; set; }

        [DataMember] public double ApparentPowerMin { get; set; }
        [DataMember] public double ApparentPowerAvg { get; set; }
        [DataMember] public double ApparentPowerMax { get; set; }

        [DataMember] public double FrequencyMin { get; set; }
        [DataMember] public double FrequencyAvg { get; set; }
        [DataMember] public double FrequencyMax { get; set; }

        [DataMember] public int RowIndex { get; set; }
        [DataMember] public string VehicleId { get; set; }

        public byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8))
            {
                w.Write(Timestamp.Ticks);
                w.Write(VoltageMin); w.Write(VoltageAvg); w.Write(VoltageMax);
                w.Write(CurrentMin); w.Write(CurrentAvg); w.Write(CurrentMax);
                w.Write(RealPowerMin); w.Write(RealPowerAvg); w.Write(RealPowerMax);
                w.Write(ReactivePowerMin); w.Write(ReactivePowerAvg); w.Write(ReactivePowerMax);
                w.Write(ApparentPowerMin); w.Write(ApparentPowerAvg); w.Write(ApparentPowerMax);
                w.Write(FrequencyMin); w.Write(FrequencyAvg); w.Write(FrequencyMax);
                w.Write(RowIndex);
                string vehicleIdSafe = VehicleId;
                if (vehicleIdSafe == null)
                    vehicleIdSafe = string.Empty;
                w.Write(vehicleIdSafe);
                w.Flush();
                return ms.ToArray();
            }
        }

        public static ChargingData FromBytes(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader r = new BinaryReader(ms, Encoding.UTF8))
            {
                return new ChargingData
                {
                    Timestamp        = new DateTime(r.ReadInt64()),
                    VoltageMin       = r.ReadDouble(),
                    VoltageAvg       = r.ReadDouble(),
                    VoltageMax       = r.ReadDouble(),
                    CurrentMin       = r.ReadDouble(),
                    CurrentAvg       = r.ReadDouble(),
                    CurrentMax       = r.ReadDouble(),
                    RealPowerMin     = r.ReadDouble(),
                    RealPowerAvg     = r.ReadDouble(),
                    RealPowerMax     = r.ReadDouble(),
                    ReactivePowerMin = r.ReadDouble(),
                    ReactivePowerAvg = r.ReadDouble(),
                    ReactivePowerMax = r.ReadDouble(),
                    ApparentPowerMin = r.ReadDouble(),
                    ApparentPowerAvg = r.ReadDouble(),
                    ApparentPowerMax = r.ReadDouble(),
                    FrequencyMin     = r.ReadDouble(),
                    FrequencyAvg     = r.ReadDouble(),
                    FrequencyMax     = r.ReadDouble(),
                    RowIndex         = r.ReadInt32(),
                    VehicleId        = r.ReadString()
                };
            }
        }
    }

    [DataContract]
    public class ChargingFault
    {
        [DataMember] public string Message { get; set; }

        public ChargingFault(string message)
        {
            Message = message;
        }
    }
}
