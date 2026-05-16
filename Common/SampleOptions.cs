using System;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SampleOptions : IDisposable
    {
        [DataMember] public byte[] Data { get; set; }

        private bool _disposed;

        public SampleOptions() { }

        public SampleOptions(byte[] data)
        {
            Data = data;
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
                    Data = null;
                }
                _disposed = true;
            }
        }

        ~SampleOptions()
        {
            Dispose(false);
        }
    }
}
