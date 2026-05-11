using System;
using System.Runtime.Serialization;

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
