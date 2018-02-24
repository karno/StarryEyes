using System.Threading.Tasks;
using Cadena.Engine;

namespace StarryEyes.Models.Subsystems
{
    public static class RequestManager
    {
        private static readonly RequestEngine _engine;

        static RequestManager()
        {
            _engine = new RequestEngine(8);
        }

        public static Task Send(IRequest request)
        {
            return _engine.SendRequest(request);
        }

        public static Task<T> Enqueue<T>(IRequest<T> request)
        {
            return _engine.SendRequest(request);
        }
    }
}