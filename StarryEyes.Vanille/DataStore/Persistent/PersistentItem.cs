
namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentItem<T>
    {
        public PersistentItem(T item)
        {
            this.item = item;
        }

        private T item;
        /// <summary>
        /// actual item
        /// </summary>
        public T Item
        {
            get { return item; }
            set
            {
                item = value;
                writeFlag = false;
            }
        }

        private bool writeFlag;
        /// <summary>
        /// flag of item is not changed
        /// </summary>
        public bool WriteFlag
        {
            get { return writeFlag; }
            set { writeFlag = value; }
        }
    }
}
