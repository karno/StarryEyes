using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Albireo.Collections;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public static class NotificationLatch
    {
        private const int BlockCount = 4;

        private static readonly object _syncRoot = new object();

        private static readonly NotificationLatchBlock[] _blocks;

        private static int _currentCount;

        static NotificationLatch()
        {
            _blocks = Enumerable.Range(0, BlockCount)
                               .Select(_ => new NotificationLatchBlock())
                               .ToArray();
        }

        public static void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(20))
                      .Subscribe(_ => ChangeNextBlock());
        }

        private static void ChangeNextBlock()
        {
            // refresh block
            lock (_syncRoot)
            {
                _currentCount = (_currentCount + 1) % BlockCount;
                _blocks[_currentCount] = new NotificationLatchBlock();
            }
        }

        public static bool CheckSetPositive(NotificationLatchTarget target, long value1, long value2)
        {
            lock (_syncRoot)
            {
                var failed = false;
                for (var i = 0; i < BlockCount; i++)
                {
                    if (!_blocks[i].CheckSetPositiveBlock(target, value1, value2))
                    {
                        failed = true;
                    }
                }
                return !failed;
            }
        }

        public static bool CheckSetNegative(NotificationLatchTarget target, long value1, long value2)
        {
            lock (_syncRoot)
            {
                lock (_syncRoot)
                {
                    var failed = false;
                    for (var i = 0; i < BlockCount; i++)
                    {
                        if (!_blocks[i].CheckSetNegativeBlock(target, value1, value2))
                        {
                            failed = true;
                        }
                    }
                    return !failed;
                }
            }
        }

        class NotificationLatchBlock
        {
            private readonly NotificationLatchUnit[] _units;

            public NotificationLatchBlock()
            {
                _units = new[]
                {
                    new NotificationLatchUnit(),
                    new NotificationLatchUnit(),
                    new NotificationLatchUnit()
                };
            }

            public bool CheckSetPositiveBlock(NotificationLatchTarget target, long value1, long value2)
            {
                return _units[(int)target].CheckSetPositive(value1, value2);
            }

            public bool CheckSetNegativeBlock(NotificationLatchTarget target, long value1, long value2)
            {
                return _units[(int)target].CheckSetNegative(value1, value2);
            }
        }

        private class NotificationLatchUnit
        {
            private readonly Dictionary<long, AVLTree<long>> _positive = new Dictionary<long, AVLTree<long>>();
            private readonly Dictionary<long, AVLTree<long>> _negative = new Dictionary<long, AVLTree<long>>();

            public bool CheckSetPositive(long value1, long value2)
            {
                if (!CheckAdd(_positive, value1, value2)) return false;
                this.CheckRemove(this._negative, value1, value2);
                return true;
            }

            public bool CheckSetNegative(long value1, long value2)
            {
                if (!CheckAdd(_negative, value1, value2)) return false;
                this.CheckRemove(this._positive, value1, value2);
                return true;
            }

            private bool CheckAdd(Dictionary<long, AVLTree<long>> dict, long v1, long v2)
            {
                AVLTree<long> tree;
                if (!dict.TryGetValue(v1, out tree))
                {
                    dict[v1] = tree = new AVLTree<long>();
                }
                return tree.AddDistinct(v2);
            }

            private void CheckRemove(Dictionary<long, AVLTree<long>> dict, long v1, long v2)
            {
                AVLTree<long> tree;
                if (dict.TryGetValue(v1, out tree))
                {
                    tree.Remove(v2);
                }
            }
        }
    }

    public enum NotificationLatchTarget
    {
        Follow,
        Block,
        Favorite,
        Mute
    }

}
