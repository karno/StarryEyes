using System;

namespace Starcluster
{
    public interface IMultiplexPredicate<in T>
    {
        Func<T, bool> GetEvaluator();

        string GetSqlQuery();
    }
}