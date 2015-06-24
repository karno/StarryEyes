using System;
using System.Collections.Concurrent;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using StarryEyes.Albireo.Threading;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public static class RequestQueue
    {
        private static readonly ConcurrentDictionary<long, TaskFactory> _accountTaskFactories =
            new ConcurrentDictionary<long, TaskFactory>();

        private static TaskFactory GetTaskFactory(this TwitterAccount account)
        {
            return _accountTaskFactories.GetOrAdd(account.Id, _ => LimitedTaskScheduler.GetTaskFactory(1));
        }

        public static Task<T> EnqueueAsync<T>(TwitterAccount account, RequestBase<T> request)
        {
            return account.GetTaskFactory().StartNew(async () =>
            {
                var retryCount = 0;
                do
                {
                    try
                    {
                        return (await request.Send(account)).Result;
                    }
                    catch (WebException ex)
                    {
                        if (!CheckTemporaryError(ex) || retryCount >= request.RetryCount)
                        {
                            // request is something wrong.
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        if (retryCount >= request.RetryCount)
                        {
                            // request is something wrong.
                            throw;
                        }
                    }
                    retryCount++;
                    // wait retry delay
                    await Task.Delay(TimeSpan.FromSeconds(request.RetryDelaySec));
                } while (true);
            }).Unwrap();
        }

        public static IObservable<T> EnqueueObservable<T>(TwitterAccount account, RequestBase<T> request)
        {
#pragma warning disable 4014
            var subject = new Subject<T>();
            account.GetTaskFactory().StartNew(async () =>
            {
                Exception thrown;
                var retryCount = 0;
                do
                {
                    try
                    {
                        var result = (await request.Send(account)).Result;
                        Task.Run(() =>
                        {
                            subject.OnNext(result);
                            subject.OnCompleted();
                        });
                        return;
                    }
                    catch (WebException ex)
                    {
                        thrown = ex;
                        if (!CheckTemporaryError(ex))
                        {
                            // request is something wrong.
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        thrown = ex;
                    }
                    retryCount++;
                    if (retryCount < request.RetryCount)
                    {
                        // wait retry delay
                        await Task.Delay(TimeSpan.FromSeconds(request.RetryDelaySec));
                    }
                } while (retryCount < request.RetryCount);
                // throw last exception
                Task.Run(() => subject.OnError(thrown));
            });
            return subject.AsObservable();
#pragma warning restore 4014
        }

        private static bool CheckTemporaryError(WebException wex)
        {
            HttpWebResponse response;
            if (wex.Status != WebExceptionStatus.ProtocolError ||
                 (response = wex.Response as HttpWebResponse) == null)
            {
                return true;
            }
            switch (response.StatusCode)
            {
                case HttpStatusCode.RequestTimeout:
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Unauthorized: // ?
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.BadGateway:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.GatewayTimeout:
                    return true;
                default:
                    return false;
            }
        }
    }
}
