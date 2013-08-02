using System;

namespace StarryEyes.Casket
{
    public interface IMultiplexPredicate<in T>
    {
        Func<T, bool> GetObjectQuery();

        string GetSqlQuery();
    }
}
