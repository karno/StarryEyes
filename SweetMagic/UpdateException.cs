using System;

namespace SweetMagic
{
    public class UpdateException : Exception
    {
        private readonly string _reason;

        public UpdateException(string reason)
        {
            this._reason = reason;
        }

        public string Reason
        {
            get { return this._reason; }
        }
    }
}
