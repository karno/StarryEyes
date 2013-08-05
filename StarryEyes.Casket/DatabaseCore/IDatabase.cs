using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarryEyes.Casket.DatabaseCore
{
    interface IDatabase<T> where T : class
    {
        Task<T> Get(long key);

        Task<IEnumerable<T>> Find(IMultiplexPredicate<T> predicate);

        Task Store(T item);

        Task Delete(long key);

        Task Delete(IMultiplexPredicate<T> predicate);
    }
}
