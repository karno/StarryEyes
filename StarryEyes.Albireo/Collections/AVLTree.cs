using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Albireo.Collections
{
    /// <summary>
    /// Implementation of the AVLTree.
    /// </summary>
    /// <typeparam name="T">Item class</typeparam>
    public sealed class AVLTree<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly IComparer<T> _comparer;

        public AVLTree() : this(Comparer<T>.Default) { }

        public AVLTree(IEnumerable<T> initial)
            : this(Comparer<T>.Default)
        {
            initial.ForEach(this.Add);
        }

        public AVLTree(IComparer<T> comparer)
        {
            this._comparer = comparer;
        }

        private int _count;

        private AVLTreeLeaf _root;

        public void Add(T item)
        {
            this.AddDistinct(item);
        }

        public bool AddDistinct(T item)
        {
            if (this._count == 0)
            {
                this._root = new AVLTreeLeaf { Value = item, Label = AVLLabel.E };
            }
            else
            {
                var current = this._root;
                var trace = new Stack<Tuple<AVLTreeLeaf, Direction>>();
                while (true)
                {
                    var d = this._comparer.Compare(current.Value, item);
                    if (d == 0) // this item is already inserted.
                    {
                        return false;
                    }
                    if (d > 0)
                    {
                        trace.Push(Tuple.Create(current, Direction.Left));
                        if (current.LeftLeaf != null)
                        {
                            current = current.LeftLeaf;
                        }
                        else
                        {
                            current.LeftLeaf = new AVLTreeLeaf { Value = item, Label = AVLLabel.E };
                            break;
                        }
                    }
                    else
                    {
                        trace.Push(Tuple.Create(current, Direction.Right));
                        if (current.RightLeaf != null)
                        {
                            current = current.RightLeaf;
                        }
                        else
                        {
                            current.RightLeaf = new AVLTreeLeaf { Value = item, Label = AVLLabel.E };
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
                        if (tuple.Item1.Label == AVLLabel.R)
                        {
                            node = node.RightLeaf.Label == AVLLabel.L ?
                                this.DoubleRightTurn(node) : this.SimpleLeftTurn(node);
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
                        if (tuple.Item1.Label == AVLLabel.L)
                        {
                            node = node.LeftLeaf.Label == AVLLabel.R ?
                                this.DoubleLeftTurn(node) : this.SimpleRightTurn(node);
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
                        if (peek.Item2 == Direction.Left)
                            peek.Item1.LeftLeaf = node;
                        else
                            peek.Item1.RightLeaf = node;
                    }
                    else
                    {
                        this._root = node;
                    }
                    break; // END
                }
            }
            this._count++;
            return true;
        }

        public bool Remove(T item)
        {
            if (this._count == 0)
            {
                return false;
            }
            // find node
            var current = this._root;
            var trace = new Stack<Tuple<AVLTreeLeaf, Direction>>();
            while (true)
            {
                var d = this._comparer.Compare(current.Value, item);
                if (d == 0) // this item is already inserted.
                {
                    // found
                    break;
                }
                if (d > 0)
                {
                    trace.Push(Tuple.Create(current, Direction.Left));

                    if (current.LeftLeaf != null)
                        current = current.LeftLeaf;
                    else // not found
                        return false;
                }
                else
                {
                    trace.Push(Tuple.Create(current, Direction.Right));

                    if (current.RightLeaf != null)
                        current = current.RightLeaf;
                    else // not found
                        return false;
                }
            }

            // remove node
            if (current.LeftLeaf != null && current.RightLeaf != null)
            {
                // has two children
                trace.Push(Tuple.Create(current, Direction.Left));

                // get most right element in the left sub-tree
                var child = current.LeftLeaf;
                while (child.RightLeaf != null)
                {
                    trace.Push(Tuple.Create(child, Direction.Right));
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
                if (trace.Count == 0)
                {
                    // top of the tree
                    this._root = current.LeftLeaf ?? current.RightLeaf;
                }
                else
                {
                    var parent = trace.Peek();
                    // has a child or no children
                    AVLTreeLeaf leaf;
                    if (current.LeftLeaf != null)
                        leaf = current.LeftLeaf;
                    else if (current.RightLeaf != null)
                        leaf = current.RightLeaf;
                    else // has no children
                        leaf = null;

                    if (parent.Item2 == Direction.Left)
                        parent.Item1.LeftLeaf = leaf;
                    else
                        parent.Item1.RightLeaf = leaf;
                }
            }

            // rotate and balance
            while (trace.Count > 0)
            {
                var breakLoop = false;
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
                    if (tuple.Item1.Label == AVLLabel.R)
                    {
                        if (node.RightLeaf.Label == AVLLabel.L)
                        {
                            node = this.DoubleRightTurn(node);
                        }
                        else
                        {
                            node = this.SimpleLeftTurn(node);
                            breakLoop = node.Label == AVLLabel.L;
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
                    if (tuple.Item1.Label == AVLLabel.L)
                    {
                        if (node.LeftLeaf.Label == AVLLabel.R)
                        {
                            node = this.DoubleLeftTurn(node);
                        }
                        else
                        {
                            node = this.SimpleRightTurn(node);
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
                    if (peek.Item2 == Direction.Left)
                        peek.Item1.LeftLeaf = node;
                    else
                        peek.Item1.RightLeaf = node;
                }
                else
                {
                    this._root = node;
                }
                if (breakLoop)
                    break;
            }
            this._count--;
            return true;
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
                newtop.Label = AVLLabel.L;
                leaf.Label = AVLLabel.R;
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
            this._root = null;
            this._count = 0;
        }

        public bool Contains(T item)
        {
            if (this._root == null) return false; // collection is empty.
            var current = this._root;
            while (true)
            {
                var d = this._comparer.Compare(current.Value, item);
                if (d == 0)
                {
                    return true;
                }
                if (d > 0)
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
            get { return this._count; }
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
            return this.GetEnumerator();
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
            readonly AVLTree<T> _target;
            readonly Stack<Tuple<AVLTreeLeaf, Direction>> _trace = new Stack<Tuple<AVLTreeLeaf, Direction>>();

            public AVLEnumerator(AVLTree<T> tree)
            {
                this._target = tree;
            }

            public T Current
            {
                get
                {
                    return this._trace.Count > 0 ? this._trace.Peek().Item1.Value : default(T);
                }
            }

            public void Dispose() { }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                if (this._target._root == null)
                {
                    return false;
                }
                if (this._trace.Count == 0) // init
                {
                    var leaf = this._target._root;
                    do
                    {
                        this._trace.Push(Tuple.Create(leaf, Direction.Left));
                        leaf = leaf.LeftLeaf;
                    } while (leaf != null);
                    return this._trace.Count > 0;
                }
                else // trace
                {
                    var leaf = this._trace.Pop().Item1;
                    if (leaf.RightLeaf != null)
                    {
                        this._trace.Push(Tuple.Create(leaf, Direction.Right));
                        leaf = leaf.RightLeaf;
                        do
                        {
                            this._trace.Push(Tuple.Create(leaf, Direction.Left));
                            leaf = leaf.LeftLeaf;
                        } while (leaf != null);
                        return true;
                    }
                    while (this._trace.Count > 0 && this._trace.Peek().Item2 == Direction.Right)
                        this._trace.Pop();
                    return this._trace.Count > 0;
                }
            }

            public void Reset()
            {
                this._trace.Clear();
            }
        }
    }
}