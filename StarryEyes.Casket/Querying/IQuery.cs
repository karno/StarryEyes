using System;

namespace StarryEyes.Casket.Querying
{
    public interface IQuery<T>
    {
        string ToSqlQuery();

        Func<T, bool> ToFilterFunc();

        long? KeyUpperBound { get; }

        long? KeyLowerBound { get; }
    }
}
