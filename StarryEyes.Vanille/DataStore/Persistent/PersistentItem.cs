
namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentItem<T>
    {
        public PersistentItem(T item)
        {
            this._item = item;
        }

        private T _item;
        /// <summary>
        /// actual item
        /// </summary>
        public T Item
        {
            get { return _item; }
            set
            {
                _item = value;
                _writeFlag = false;
            }
        }

        private bool _writeFlag;
        /// <summary>
        /// flag of item is not changed
        /// </summary>
        public bool WriteFlag
        {
            get { return _writeFlag; }
            set { _writeFlag = value; }
        }
    }
}
