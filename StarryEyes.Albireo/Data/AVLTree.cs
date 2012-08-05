using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarryEyes.Albireo.Data
{
    /// <summary>
    /// Implementation of the AVLTree.
    /// </summary>
    /// <typeparam name="T">Item class</typeparam>
    public sealed class AVLTree<T> : ICollection<T> where T : IComparable<T>
    {
        private int count = 0;

        private AVLTreeLeaf root;

        public void Add(T item)
        {
            if (count == 0)
            {
                root = new AVLTreeLeaf() { Value = item, Label = AVLLabel.E };
            }
            else
            {
                AVLTreeLeaf current = root;
                Stack<Tuple<AVLTreeLeaf, Direction>> trace = new Stack<Tuple<AVLTreeLeaf, Direction>>();
                while (true)
                {
                    int d = current.Value.CompareTo(item);
                    if (d == 0) // this item is already inserted.
                    {
                        return;
                    }
                    else if (d > 0)
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(current, Direction.Left));
                        if (current.LeftLeaf != null)
                        {
                            current = current.LeftLeaf;
                        }
                        else
                        {
                            current.LeftLeaf = new AVLTreeLeaf() { Value = item, Label = AVLLabel.E };
                            break;
                        }
                    }
                    else
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(current, Direction.Right));
                        if (current.RightLeaf != null)
                        {
                            current = current.RightLeaf;
                        }
                        else
                        {
                            current.RightLeaf = new AVLTreeLeaf() { Value = item, Label = AVLLabel.E };
                            break;
                        }
                    }
                }

                // rotate and balance
                while (trace.Count > 0)
                {
                    var tuple = trace.Pop();
                    var node = tuple.Item1;
                    if (tuple.Item2 == Direction.Right)
                    {
                        // come from right
                        if (tuple.Item1.Label == AVLLabel.E) // CONTINUE
                        {
                            tuple.Item1.Label = AVLLabel.R;
                            continue;
                        }
                        else if (tuple.Item1.Label == AVLLabel.R)
                        {
                            if (node.RightLeaf.Label == AVLLabel.L)
                                node = DoubleRightTurn(node);
                            else
                                node = SimpleLeftTurn(node);
                        }
                        else
                        {
                            tuple.Item1.Label = AVLLabel.E;
                        }
                    }
                    else
                    {
                        // come from left
                        if (tuple.Item1.Label == AVLLabel.E) // CONTINUE
                        {
                            tuple.Item1.Label = AVLLabel.L;
                            continue;
                        }
                        else if (tuple.Item1.Label == AVLLabel.L)
                        {
                            if (node.LeftLeaf.Label == AVLLabel.R)
                                node = DoubleLeftTurn(node);
                            else
                                node = SimpleRightTurn(node);
                        }
                        else
                        {
                            tuple.Item1.Label = AVLLabel.E;
                        }
                    }

                    // attach new node 
                    if (trace.Count > 0)
                    {
                        var peek = trace.Peek();
                        switch (peek.Item2)
                        {
                            case Direction.Left:
                                peek.Item1.LeftLeaf = node;
                                break;
                            case Direction.Right:
                                peek.Item1.RightLeaf = node;
                                break;
                        }
                    }
                    else
                    {
                        root = node;
                    }
                    break; // END
                }
            }
            count++;
        }

        public bool Remove(T item)
        {
            if (count == 0)
            {
                return false;
            }
            else
            {
                // find node
                AVLTreeLeaf current = root;
                Stack<Tuple<AVLTreeLeaf, Direction>> trace = new Stack<Tuple<AVLTreeLeaf, Direction>>();
                while (true)
                {
                    int d = current.Value.CompareTo(item);
                    if (d == 0) // this item is already inserted.
                    {
                        // found
                        break;
                    }
                    else if (d > 0)
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(current, Direction.Left));

                        if (current.LeftLeaf != null)
                            current = current.LeftLeaf;
                        else // not found
                            return false;
                    }
                    else
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(current, Direction.Right));

                        if (current.RightLeaf != null)
                            current = current.RightLeaf;
                        else // not found
                            return false;
                    }
                }

                // remove node
                var parent = trace.Peek();
                if (current.LeftLeaf != null && current.RightLeaf != null)
                {
                    // has two children
                    trace.Push(new Tuple<AVLTreeLeaf, Direction>(current, Direction.Left));

                    // get most right element in the left sub-tree
                    var child = current.LeftLeaf;
                    while (child.RightLeaf != null)
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(child, Direction.Right));
                        child = child.RightLeaf;
                    }
                    // remove most-right children
                    var peek = trace.Peek();
                    if (peek.Item2 == Direction.Left)
                        trace.Peek().Item1.LeftLeaf = child.LeftLeaf;
                    else
                        trace.Peek().Item1.RightLeaf = child.LeftLeaf;
                    current.Value = child.Value;
                }
                else
                {
                    // has a child or no children
                    AVLTreeLeaf leaf = null;
                    if (current.LeftLeaf != null)
                        leaf = current.RightLeaf;
                    else if (current.RightLeaf != null)
                        leaf = current.LeftLeaf;
                    else // has no children
                        leaf = null;

                    switch (parent.Item2)
                    {
                        case Direction.Left:
                            parent.Item1.LeftLeaf = leaf;
                            break;
                        case Direction.Right:
                            parent.Item1.RightLeaf = leaf;
                            break;
                    }
                }
                
                // rotate and balance
                while (trace.Count > 0)
                {
                    bool breakLoop = false;
                    var tuple = trace.Pop();
                    var node = tuple.Item1;
                    if (tuple.Item2 == Direction.Left)
                    {
                        // come from left
                        if (tuple.Item1.Label == AVLLabel.L)
                        {
                            tuple.Item1.Label = AVLLabel.E;
                            continue;
                        }
                        else if (tuple.Item1.Label == AVLLabel.R)
                        {
                            if (node.RightLeaf.Label == AVLLabel.L)
                            {
                                node = DoubleRightTurn(node);
                            }
                            else
                            {
                                node = SimpleLeftTurn(node);
                                breakLoop = node.Label != AVLLabel.E;
                            }
                        }
                        else
                        {
                            tuple.Item1.Label = AVLLabel.R;
                            break;
                        }
                    }
                    else
                    {
                        // come from right
                        if (tuple.Item1.Label == AVLLabel.R)
                        {
                            tuple.Item1.Label = AVLLabel.E;
                            continue;
                        }
                        else if (tuple.Item1.Label == AVLLabel.L)
                        {
                            if (node.LeftLeaf.Label == AVLLabel.R)
                            {
                                node = DoubleRightTurn(node);
                            }
                            else
                            {
                                node = SimpleRightTurn(node);
                                breakLoop = node.Label != AVLLabel.E;
                            }
                        }
                        else
                        {
                            tuple.Item1.Label = AVLLabel.L;
                            break;
                        }
                    }

                    // attach new node 
                    if (trace.Count > 0)
                    {
                        var peek = trace.Peek();
                        switch (peek.Item2)
                        {
                            case Direction.Left:
                                peek.Item1.LeftLeaf = node;
                                break;
                            case Direction.Right:
                                peek.Item1.RightLeaf = node;
                                break;
                        }
                    }
                    else
                    {
                        root = node;
                    }
                    if (breakLoop)
                        break;
                }
                return true;
            }
        }

        private AVLTreeLeaf SimpleLeftTurn(AVLTreeLeaf leaf)
        {
            if (leaf == null)
                throw new ArgumentNullException("leaf");
            var newtop = leaf.RightLeaf;
            leaf.RightLeaf = newtop.LeftLeaf;
            newtop.LeftLeaf = leaf;
            if (newtop.Label == AVLLabel.E)
            {
                newtop.Label = AVLLabel.R;
                leaf.Label = AVLLabel.L;
            }
            else
            {
                newtop.Label = AVLLabel.E;
                leaf.Label = AVLLabel.E;
            }
            return newtop;
        }

        private AVLTreeLeaf DoubleLeftTurn(AVLTreeLeaf leaf)
        {
            if (leaf == null)
                throw new ArgumentNullException("leaf");
            var newtop = leaf.LeftLeaf.RightLeaf;
            var beta = newtop.LeftLeaf;
            var gamma = newtop.RightLeaf;
            newtop.RightLeaf = leaf;
            newtop.LeftLeaf = leaf.LeftLeaf;
            newtop.LeftLeaf.RightLeaf = beta;
            newtop.RightLeaf.LeftLeaf = gamma;
            newtop.LeftLeaf.Label = newtop.Label == AVLLabel.R ? AVLLabel.L : AVLLabel.E;
            newtop.RightLeaf.Label = newtop.Label == AVLLabel.L ? AVLLabel.R : AVLLabel.E;
            newtop.Label = AVLLabel.E;
            return newtop;
        }

        private AVLTreeLeaf SimpleRightTurn(AVLTreeLeaf leaf)
        {
            if (leaf == null)
                throw new ArgumentNullException("leaf");
            var newtop = leaf.LeftLeaf;
            leaf.LeftLeaf = newtop.RightLeaf;
            newtop.RightLeaf = leaf;
            if (newtop.Label == AVLLabel.E)
            {
                newtop.Label = AVLLabel.R;
                leaf.Label = AVLLabel.L;
            }
            else
            {
                newtop.Label = AVLLabel.E;
                leaf.Label = AVLLabel.E;
            }
            return newtop;
        }

        private AVLTreeLeaf DoubleRightTurn(AVLTreeLeaf leaf)
        {
            if (leaf == null)
                throw new ArgumentNullException("leaf");
            var newtop = leaf.RightLeaf.LeftLeaf;
            var beta = newtop.LeftLeaf;
            var gamma = newtop.RightLeaf;
            newtop.LeftLeaf = leaf;
            newtop.RightLeaf = leaf.RightLeaf;
            newtop.LeftLeaf.RightLeaf = beta;
            newtop.RightLeaf.LeftLeaf = gamma;
            newtop.LeftLeaf.Label = newtop.Label == AVLLabel.R ? AVLLabel.L : AVLLabel.E;
            newtop.RightLeaf.Label = newtop.Label == AVLLabel.L ? AVLLabel.R : AVLLabel.E;
            newtop.Label = AVLLabel.E;
            return newtop;
        }

        public void Clear()
        {
            root = null;
            count = 0;
        }

        public bool Contains(T item)
        {
            AVLTreeLeaf current = root;
            while (true)
            {
                int d = current.Value.CompareTo(item);
                if (d == 0)
                {
                    return true;
                }
                else if (d < 0)
                {
                    if (current.LeftLeaf != null)
                        current = current.LeftLeaf;
                    else
                        return false;
                }
                else
                {
                    if (current.RightLeaf != null)
                        current = current.RightLeaf;
                    else
                        return false;
                }
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        public int Count
        {
            get { return count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new AVLEnumerator(this); 
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class AVLTreeLeaf : TreeLeaf<T, AVLTreeLeaf>
        {
            public AVLLabel Label { get; set; }
        }

        /// <summary>
        /// AVL Leaf state
        /// </summary>
        enum AVLLabel
        {
            L,
            E,
            R,
        }

        enum Direction
        {
            Left,
            Right,
        }

        class AVLEnumerator : IEnumerator<T>
        {
            AVLTree<T> target;
            Stack<Tuple<AVLTreeLeaf, Direction>> trace = new Stack<Tuple<AVLTreeLeaf, Direction>>();

            public AVLEnumerator(AVLTree<T> tree)
            {
                target = tree;
            }

            public T Current
            {
                get
                {
                    return trace.Count > 0 ? trace.Peek().Item1.Value : default(T);
                }
            }

            public void Dispose() { }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (trace.Count == 0) // init
                {
                    AVLTreeLeaf leaf = target.root;
                    do
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(leaf, Direction.Left));
                        leaf = leaf.LeftLeaf;
                    } while (leaf != null) ;
                    return trace.Count > 0;
                }
                else // trace
                {
                    var leaf = trace.Pop().Item1;
                    if (leaf.RightLeaf != null)
                    {
                        trace.Push(new Tuple<AVLTreeLeaf, Direction>(leaf, Direction.Right));
                        leaf = leaf.RightLeaf;
                        do
                        {
                            trace.Push(new Tuple<AVLTreeLeaf, Direction>(leaf, Direction.Left));
                            leaf = leaf.LeftLeaf;
                        } while (leaf != null);
                        return true;
                    }
                    else 
                    {
                        while (trace.Count > 0 && trace.Peek().Item2 == Direction.Right)
                            trace.Pop();
                    }
                    return trace.Count > 0;
                }
            }

            public void Reset()
            {
                trace.Clear();
            }
        }

    }
}
