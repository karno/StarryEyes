using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Cursoring
    {
        public static async Task<IEnumerable<T>> RetrieveAllCursor<T>(
            this IOAuthCredential credential,
            Func<IOAuthCredential, long, Task<ICursorResult<IEnumerable<T>>>> reader)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (reader == null) throw new ArgumentNullException("reader");
            var result = new List<T>();
            long cursor = 0;
            while (true)
            {
                var cr = await reader(credential, cursor);
                result.AddRange(cr.Result);
                if (!cr.CanReadNext)
                {
                    break;
                }
                cursor = cr.NextCursor;
            }
            return result;
        }
    }
}
