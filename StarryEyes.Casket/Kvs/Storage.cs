using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StarryEyes.Casket.Kvs
{
    /// <summary>
    /// Store/Find binary data
    /// </summary>
    public class Storage : IDisposable
    {
        private readonly string _path;
        private readonly SortedDictionary<long, Tuple<long, int>> _storeMap = new SortedDictionary<long, Tuple<long, int>>();
        private readonly FileStream _filestream;
        private long _tail;

        private string ControlPath
        {
            get { return _path + "m"; }
        }

        public Storage(string path)
        {
            _path = path;
            _filestream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.ReadWrite);
        }

        public async Task Store(long id, byte[] data)
        {
            long offset;
            lock (_storeMap)
            {
                Tuple<long, int> tuple;
                if (_storeMap.TryGetValue(id, out tuple) &&
                    tuple.Item2 >= data.Length)
                {
                    _storeMap[id] = Tuple.Create(tuple.Item1, data.Length);
                    offset = tuple.Item1;
                }
                else
                {
                    offset = _tail;
                    _tail += data.Length;
                    _storeMap[id] = Tuple.Create(offset, data.Length);
                }
            }
            await this.StoreInternal(offset, data);
        }

        public async Task<byte[]> Load(long id)
        {
            Tuple<long, int> tuple;
            lock (_storeMap)
            {
                if (!_storeMap.TryGetValue(id, out tuple))
                {
                    return null;
                }
            }
            return await this.LoadInternal(tuple.Item1, tuple.Item2);
        }

        public async Task WriteManageTable()
        {
            throw new NotImplementedException();
        }

        public async Task<string> Compact()
        {
            throw new NotImplementedException();
        }

        private async Task StoreInternal(long offset, byte[] data)
        {
            Task task;
            lock (_filestream)
            {
                _filestream.Seek(offset, SeekOrigin.Begin);
                task = this._filestream.WriteAsync(data, 0, data.Length);
            }
            await task;
        }

        private async Task<byte[]> LoadInternal(long offset, int length)
        {
            var buffer = new byte[length];
            Task<int> task;
            lock (_filestream)
            {
                _filestream.Seek(offset, SeekOrigin.Begin);
                task = this._filestream.ReadAsync(buffer, 0, length);
            }
            var readsize = await task;
            Array.Resize(ref buffer, readsize);
            return buffer;
        }

        private bool _isDisposed;

        private void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Storage");
            }
        }

        public virtual void Dispose()
        {
            this.CheckDisposed();
            this.Dispose(true);
            _isDisposed = true;
        }

        ~Storage()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposable)
        {
#pragma warning disable 4014
            if (!disposable) return;
            this._filestream.Dispose();
            this.WriteManageTable();
#pragma warning restore 4014
        }
    }
}
