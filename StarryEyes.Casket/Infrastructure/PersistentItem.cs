

namespace StarryEyes.Casket.Infrastructure
{
    internal class PersistentItem<T>
    {
        private readonly long _key;
        private bool _isDeletion;
        private T _item;
        private bool _writeFlag;

        public PersistentItem(long key, T item)
        {
            this._isDeletion = false;
            this._key = key;
            this._item = item;
        }

        public PersistentItem(long key)
        {
            this._isDeletion = true;
            this._key = key;
        }

        /// <summary>
        /// Set this item as deleted item
        /// </summary>
        public bool SetDeleted()
        {
            this._isDeletion = true;
            this._writeFlag = false;
        }

        /// <summary>
        /// Key of the item
        /// </summary>
        public long Key
        {
            get { return this._key; }
        }

        /// <summary>
        /// actual item
        /// </summary>
        public T Item
        {
            get { return _item; }
            set
            {
                this._item = value;
                this._isDeletion = false;
                this._writeFlag = false;
            }
        }

        /// <summary>
        /// Whether this item is deletion item
        /// </summary>
        public bool IsDeletion
        {
            get { return this._isDeletion; }
        }

        /// <summary>
        /// flag of item is not changed
        /// </summary>
        public bool WriteFlag
        {
            get { return this._writeFlag; }
            set { this._writeFlag = value; }
        }

    }
}
