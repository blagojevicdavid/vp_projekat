using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface IChargingService
    {
        [OperationContract]
        [FaultContract(typeof(ChargingFault))]
        void StartSession(string vehicleId);

        [OperationContract]
        [FaultContract(typeof(ChargingFault))]
        void PushSample(ChargingData data);

        [OperationContract]
        void EndSession(string vehicleId);
    }
}
