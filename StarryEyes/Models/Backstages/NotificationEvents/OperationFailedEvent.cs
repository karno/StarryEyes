using System;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents
{
    public class OperationFailedEvent : BackstageEventBase
    {
        private readonly string _description;
        private readonly Exception _exception;

        public OperationFailedEvent(string description, Exception exception)
        {
            _description = description;
            _exception = exception;
        }

        public override string Title => "FAILED";

        public override string Detail => _description +
                                         (_exception == null
                                             ? String.Empty
                                             : " - " + BuildMessage(GetCoreException(_exception)));

        private string BuildMessage(Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException != null)
            {
                msg += " - " + BuildMessage(ex.InnerException);
            }
            return msg;
        }

        private Exception GetCoreException(Exception ex)
        {
            var aex = ex as AggregateException;
            if (aex != null)
            {
                return GetCoreException(aex.Flatten().InnerExceptions[0]);
            }
            if (ex.InnerException != null)
            {
                return GetCoreException(ex.InnerException);
            }
            return ex;
        }

        public override System.Windows.Media.Color Background => MetroColors.Red;
    }
}