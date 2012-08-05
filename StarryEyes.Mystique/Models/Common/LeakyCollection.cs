using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace StarryEyes.Mystique.Models.Common
{
    /// <summary>
    /// A collection which holds limited items
    /// </summary>
    public class LeakyCollection<T> : ObservableCollection<T>
    {
        private int _threshold;
        private bool _isAllowOverCapacity;
        private LeakingStrategy _leakingStrategy;

        public LeakyCollection(int threshold, LeakingStrategy leakingStrategy)
            : base()
        {
            this._threshold = threshold;
            this._leakingStrategy = leakingStrategy;
        }

        public bool IsAllowOverCapacity
        {
            get { return this._isAllowOverCapacity; }
            set { this._isAllowOverCapacity = value; }
        }

        public LeakingStrategy LeakingStrategy
        {
            get { return this._leakingStrategy; }
            set { this._leakingStrategy = value; }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            // call after committing previous action
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // trimming exceeds
                while (this.Count > this._threshold)
                {
                    this.RemoveAt(this._leakingStrategy == LeakingStrategy.RemoveFromTop ? 0 : this.Count - 1);
                }
            }
        }
    }

    public enum LeakingStrategy
    {
        RemoveFromTop,
        RemoveFromBottom,
    }
}
