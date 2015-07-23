using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    internal sealed class CancellableStreamReader : IDisposable
    {
        private const int BufferLength = 1024;

        private readonly Stream _stream;
        private readonly Decoder _decoder;

        private readonly byte[] _byteBuffer;

        private readonly char[] _buffer;
        private int _bufferedLength;
        private int _bufferCursor;

        private bool _skipNextLineFeed;

        private bool _hasDisposed;

        public CancellableStreamReader(Stream stream)
            : this(stream, Encoding.UTF8)
        {
        }

        public CancellableStreamReader(Stream stream, Encoding encoding)
        {
            _stream = stream;
            _decoder = encoding.GetDecoder();
            _byteBuffer = new byte[BufferLength];
            _buffer = new char[encoding.GetMaxCharCount(BufferLength)];
            _bufferedLength = -1;
            _bufferCursor = -1;
        }

        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            if (_hasDisposed)
            {
                throw new ObjectDisposedException("CancellableStreamReader");
            }
            // if _bufferedLength == 0, hit to end of stream in previous read.
            if (_bufferedLength == 0) return null;
            StringBuilder builder = null;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_bufferCursor == _bufferedLength)
                {
                    await ReceiveToBufferAsync(cancellationToken).ConfigureAwait(false);
                    // hit to End of Stream and internal buffer is empty.
                    if (_bufferedLength == 0 && builder == null) return null;
                }
                cancellationToken.ThrowIfCancellationRequested();

                // check next char is '\n' if before trailing char of line is '\r'.
                if (_skipNextLineFeed && _bufferCursor < _bufferedLength && _buffer[_bufferCursor] == '\n')
                {
                    // previous receive ends with '\r', so we should ignore next '\n'.
                    _bufferCursor++;
                }
                _skipNextLineFeed = false;

                // find End of Line char (\r OR \n OR \r\n).
                for (var i = _bufferCursor; i < _bufferedLength; i++)
                {
                    if (_buffer[i] == '\r' || _buffer[i] == '\n')
                    {
                        // build return string, not contains line-feed char.
                        var rets = builder == null
                            ? new String(_buffer, _bufferCursor, i - _bufferCursor)
                            : builder.Append(_buffer, _bufferCursor, i - _bufferCursor).ToString();

                        // point next char
                        _bufferCursor = i + 1;
                        if (_buffer[i] == '\r')
                        {
                            if (i + 1 <= _bufferedLength)
                            {
                                _skipNextLineFeed = true;
                            }
                            else if (_buffer[i + 1] != '\n')
                            {
                                _bufferCursor++;
                            }
                        }
                        return rets;
                    }
                }

                // buffer not contains '\r' or '\n'.
                if (builder == null)
                {
                    builder = new StringBuilder(_buffer.Length * 2);
                }
                builder.Append(_buffer, _bufferCursor, _bufferedLength - _bufferCursor);
                // buffer cursor hit to end
                _bufferCursor = _bufferedLength;
            } while (_bufferedLength != 0); // _bufferedLength = 0 => End of Stream, break.
            return builder.ToString();
        }

        private async Task ReceiveToBufferAsync(CancellationToken cancellationToken)
        {
            do
            {
                // fill buffer from NetworkStream
                var length = await _stream.ReadAsync(_byteBuffer, 0, BufferLength,
                    cancellationToken).ConfigureAwait(false);
                // decode chars and reset cursor
                _bufferedLength = _decoder.GetChars(_byteBuffer, 0, length, _buffer, 0);
                _bufferCursor = 0;
                // length == 0 => End of Stream, break.
                if (length == 0) break;
                // if _bufferedLength is zero although _byteBuffer is not empty, loop once more.
            } while (_bufferCursor == _bufferedLength);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CancellableStreamReader()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
                _hasDisposed = true;
            }
        }

    }
}
