using System;
using System.Reactive.Linq;
using StarryEyes.Mystique.Filters;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;

namespace StarryEyes.Mystique.Models.Tab
{
    /// <summary>
    /// Manage resources for a tab.
    /// </summary>
    public class TabBackEnd 
    {
        int ChunkCount = 50;

        private Func<TwitterStatus, bool> evaluator = _ => false;
        private FilterQuery filterQuery;
        public FilterQuery FilterQuery
        {
            get { return filterQuery; }
            set
            {
                if (this.filterQuery != value)
                {
                    value.Deactivate();
                    filterQuery = value;
                    if (filterQuery != null)
                    {
                        filterQuery.Activate();
                        evaluator = filterQuery.GetEvaluator();
                    }
                    else
                    {
                        evaluator = _ => false;
                    }
                    ReFilter();
                }
            }
        }

        public void ReFilter()
        {
            var handler = OnCollectionInvalidateRequired;
            if (handler != null)
                handler();
        }

        /// <summary>
        /// Notify collection invalidated.
        /// </summary>
        public event Action OnCollectionInvalidateRequired;

        public void Deactivate()
        {
            filterQuery.Deactivate();
        }

        public void Activate()
        {
            filterQuery.Activate();
        }

        public IObservable<StatusNotification> GetFilteredStream()
        {
            return StatusStore.StatusPublisher
                .Where(sn => !sn.IsAdded || evaluator(sn.Status));
        }

        public IObservable<TwitterStatus> Get(long? maxId)
        {
            return StatusStore.Find(evaluator,
                maxId != null ? FindRange<long>.By(maxId.Value) : null,
                ChunkCount);
        }
    }
}
