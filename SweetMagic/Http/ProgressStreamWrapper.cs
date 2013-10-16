using System;
using System.IO;

namespace SweetMagic.Http
{
    public class ProgressStreamWrapper : Stream
    {
        private readonly Stream _inner;

        public ProgressStreamWrapper(Stream inner)
        {
            _inner = inner;
        }

        public event EventHandler<int> BytesTransferred;
        protected virtual void OnBytesTransferred(int e)
        {
            EventHandler<int> handler = BytesTransferred;
            if (handler != null) handler(this, e);
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = _inner.Read(buffer, offset, count);
            OnBytesTransferred(length);
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            OnBytesTransferred(count);
        }

        public override bool CanRead
        {
            get { return _inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _inner.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _inner.CanWrite; }
        }

        public override long Length
        {
            get { return _inner.Length; }
        }

        public override long Position
        {
            get { return _inner.Position; }
            set { _inner.Position = value; }
        }
    }
}
