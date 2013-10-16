using System;

namespace SweetMagic.Http
{
    public sealed class ProgressEventArgs : EventArgs
    {
        private readonly long _transferred;
        private readonly long? _totalBytes;

        public ProgressEventArgs(long transferred, long? totalBytes)
        {
            _transferred = transferred;
            _totalBytes = totalBytes;
        }

        public int? Percentage
        {
            get
            {
                return _totalBytes == null
                           ? (int?)null
                           : (int)(_transferred * 100 / _totalBytes.Value);
            }
        }

        public long Transferred
        {
            get { return _transferred; }
        }

        public long? TotalBytes
        {
            get { return _totalBytes; }
        }
    }
}
