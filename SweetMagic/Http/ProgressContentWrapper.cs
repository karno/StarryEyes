using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SweetMagic.Http
{
    public class ProgressContentWrapper : HttpContent
    {
        public event EventHandler<ProgressEventArgs> Progress;
        protected virtual void OnProgress(ProgressEventArgs e)
        {
            var handler = Progress;
            if (handler != null) handler(this, e);
        }

        private readonly HttpContent _inner;
        private long? _totalBytes;
        private long _transferredBytes;

        public ProgressContentWrapper(HttpContent inner)
        {
            _totalBytes = inner.Headers.ContentLength;
            _inner = inner;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var ps = new ProgressStreamWrapper(stream);
            ps.BytesTransferred += TransferNotify;
            return _inner.CopyToAsync(stream, context);
        }

        void TransferNotify(object sender, int length)
        {
            _transferredBytes += length;
            OnProgress(new ProgressEventArgs(_transferredBytes, _totalBytes));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = (_totalBytes = _inner.Headers.ContentLength) ?? -1;
            return length >= 0;
        }
    }
}
