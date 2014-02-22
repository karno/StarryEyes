
using System.Collections.Generic;
using StarryEyes.Albireo.Collections;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public class NotificationDuplicationDetector
    {
        private readonly Dictionary<long, AVLTree<long>> _table =
            new Dictionary<long, AVLTree<long>>();

        public bool CheckAdd(long sourceId, long targetId)
        {
            lock (_table)
            {
                AVLTree<long> tree;
                if (!_table.TryGetValue(sourceId, out tree))
                {
                    _table[sourceId] = tree = new AVLTree<long>();
                }
                return tree.AddDistinct(targetId);
            }
        }

        public bool CheckRemove(long sourceId, long targetId)
        {
            lock (_table)
            {
                AVLTree<long> tree;
                if (!_table.TryGetValue(sourceId, out tree))
                {
                    return false;
                }
                return tree.Remove(targetId);
            }
        }
    }
}
