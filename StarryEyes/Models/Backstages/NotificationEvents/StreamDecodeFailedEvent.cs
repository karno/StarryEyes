using System;
using System.Windows.Media;
using StarryEyes.Globalization;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents
{
    public class StreamDecodeFailedEvent : BackstageEventBase
    {
        private readonly string _screenName;
        private readonly Exception _exception;

        public StreamDecodeFailedEvent(string screenName, Exception exception)
        {
            this._screenName = screenName;
            this._exception = exception;
        }

        public override string Title
        {
            get { return "DECODE FAILED"; }
        }

        public override string Detail
        {
            get
            {
                return BackstageResources.StreamDecodeFailedFormat.SafeFormat("@" + _screenName, _exception.Message);
            }
        }

        public override Color Background
        {
            get { return MetroColors.Indigo; }
        }
    }
}
