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
            this._description = description;
            this._exception = exception;
        }

        public override string Title
        {
            get { return "FAILED"; }
        }

        public override string Detail
        {
            get
            {
                return this._description +
                       (this._exception == null
                           ? String.Empty
                           : " - " + this.GetCoreException(this._exception).Message);
            }
        }

        private Exception GetCoreException(Exception ex)
        {
            var aex = ex as AggregateException;
            if (aex != null)
            {
                return this.GetCoreException(aex.Flatten().InnerExceptions[0]);
            }
            if (ex.InnerException != null)
            {
                return this.GetCoreException(ex.InnerException);
            }
            return ex;
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Red; }
        }
    }
}
