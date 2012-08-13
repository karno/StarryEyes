using System;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Distributed
{
    public class DistributedDataStore<TKey, TValue> 
        : DataStoreBase<TKey, TValue> where TValue : IBinarySerializable, new()
    {
        int distribution;

        public DistributedDataStore(Func<TValue, TKey> keyProvider, int distribution)
            : base(keyProvider)
        {
            this.distribution = distribution;
        }

        public override int Count
        {
            get { throw new NotImplementedException(); }
        }

        public override void Store(TValue value)
        {
            throw new NotImplementedException();
        }

        public override IObservable<TValue> Get(TKey key)
        {
            throw new NotImplementedException();
        }

        public override IObservable<TValue> Find(Func<TValue, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public override void Remove(TKey key)
        {
            throw new NotImplementedException();
        }
    }
}
