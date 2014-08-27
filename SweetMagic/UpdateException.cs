using System;

namespace SweetMagic
{
    public class UpdateException : Exception
    {
        public UpdateException(string message)
            : base(message)
        {
        }
    }
}
