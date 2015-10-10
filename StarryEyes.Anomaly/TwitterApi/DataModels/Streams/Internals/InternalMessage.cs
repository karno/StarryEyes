using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Internals
{
    public abstract class InternalMessage : StreamMessage
    {
        protected InternalMessage() : base(DateTime.Now)
        {
        }
    }
}
