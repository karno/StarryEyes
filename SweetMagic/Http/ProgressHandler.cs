using System;
using System.Net.Http;
using System.Threading;

namespace SweetMagic.Http
{
    public class ProgressHandler : MessageProcessingHandler
    {
        public event EventHandler<ProgressEventArgs> ReceiveProgress;

        protected virtual void OnReceiveProgress(ProgressEventArgs e)
        {
            var handler = ReceiveProgress;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<ProgressEventArgs> SendProgress;

        protected virtual void OnSendProgress(ProgressEventArgs e)
        {
            EventHandler<ProgressEventArgs> handler = SendProgress;
            if (handler != null) handler(this, e);
        }

        public ProgressHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request != null)
            {
                var wrapper = new ProgressContentWrapper(request.Content);
                wrapper.Progress += (sender, e) => OnSendProgress(e);
                request.Content = wrapper;
            }
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response != null)
            {
                var wrapper = new ProgressContentWrapper(response.Content);
                wrapper.Progress += (sender, e) => OnReceiveProgress(e);
                response.Content = wrapper;
            }
            return response;
        }
    }
}
