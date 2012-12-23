using System;
using System.Runtime.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    [Serializable]
    public class DataPersistenceException : Exception
    {
        public DataPersistenceException()
        {
        }

        public DataPersistenceException(string message)
            : base(message)
        {
        }

        public DataPersistenceException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected DataPersistenceException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
