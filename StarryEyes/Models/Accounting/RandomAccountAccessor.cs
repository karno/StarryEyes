using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cadena;
using Cadena.Data;
using StarryEyes.Settings;

namespace StarryEyes.Models.Accounting
{
    public class RandomAccountAccessor : IApiAccessor
    {
        public long Id
        {
            get { return 0; }
        }

        public Task<IApiResult<T>> GetAsync<T>(string path, IDictionary<string, object> parameter,
            Func<HttpResponseMessage, Task<T>> converter, CancellationToken cancellationToken)
        {
            return Setting.Accounts.GetRandomOne().CreateAccessor()
                          .GetAsync(path, parameter, converter, cancellationToken);
        }

        public Task<IApiResult<T>> PostAsync<T>(string path, HttpContent content,
            Func<HttpResponseMessage, Task<T>> converter, CancellationToken cancellationToken)
        {
            return Setting.Accounts.GetRandomOne().CreateAccessor()
                          .PostAsync(path, content, converter, cancellationToken);
        }

        public Task<IApiResult<T>> PostAsync<T>(string path, IDictionary<string, object> parameter,
            Func<HttpResponseMessage, Task<T>> converter, CancellationToken cancellationToken)
        {
            return Setting.Accounts.GetRandomOne().CreateAccessor()
                          .PostAsync(path, parameter, converter, cancellationToken);
        }

        public Task ConnectStreamAsync(string path, IDictionary<string, object> parameter,
            Func<Stream, Task> streamReader, CancellationToken cancellationToken)
        {
            return Setting.Accounts.GetRandomOne().CreateAccessor()
                          .ConnectStreamAsync(path, parameter, streamReader, cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}