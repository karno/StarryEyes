using System;
using System.Runtime.Serialization;

namespace StarryEyes.Models.Databases
{
    [Serializable]
    public class DatabaseConsistencyException : Exception
    {
        public DatabaseConsistencyException()
        {
        }

        public DatabaseConsistencyException(string message)
            : base(message)
        {
        }

        public DatabaseConsistencyException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected DatabaseConsistencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
