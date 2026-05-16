using System;
using System.ServiceModel;
using Common;

namespace Client
{
    public class ChargingClient : IDisposable
    {
        private ChannelFactory<IChargingService> _factory;
        private IChargingService _channel;
        private bool _disposed = false;

        public ChargingClient()
        {
            _factory = new ChannelFactory<IChargingService>("ChargingService");
            _channel = _factory.CreateChannel();
        }

        public void StartSession(string vehicleId)
        {
            _channel.StartSession(vehicleId);
        }

        public void PushSample(ChargingData data)
        {
            using (SampleOptions options = new SampleOptions(data.ToBytes()))
            {
                try
                {
                    _channel.PushSample(options);
                }
                catch (FaultException<ChargingFault> ex)
                {
                    Console.WriteLine("Sample rejected: " + ex.Detail.Message);
                }
            }
        }

        public void EndSession(string vehicleId)
        {
            _channel.EndSession(vehicleId);
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
                    try
                    {
                        if (_factory != null && _factory.State != CommunicationState.Faulted)
                            _factory.Close();
                        else if (_factory != null)
                            _factory.Abort();
                    }
                    catch
                    {
                        if (_factory != null)
                            _factory.Abort();
                    }
                }
                _disposed = true;
            }
        }

        ~ChargingClient()
        {
            Dispose(false);
        }
    }
}
