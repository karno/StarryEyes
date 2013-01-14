using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Stores;
using StarryEyes.Vanille.DataStore;

namespace StarryEyes.Models.Tab
{
    /// <summary>
    ///     Hold tab information for spawning tab.
    /// </summary>
    public sealed class TabModel
    {
        private readonly AVLTree<long> _bindingAccountIds = new AVLTree<long>();
        private List<string> _bindingHashtags = new List<string>();
        private Func<TwitterStatus, bool> _evaluator = _ => false;
        private FilterQuery _filterQuery;

        public TabModel()
        {
        }

        public TabModel(string name, string query)
            : this()
        {
            Name = name;
            FilterQueryString = query;
        }

        /// <summary>
        ///     Name of this tab.
        /// </summary>
        public string Name { get; set; }

        public event Action OnBindingAccountIdsChanged;

        private void RaiseOnBindingAccountIdsChanged()
        {
            Action handler = OnBindingAccountIdsChanged;
            if (handler != null) handler();
        }

        /// <summary>
        ///     Binding accounts ids
        /// </summary>
        public ICollection<long> BindingAccountIds
        {
            get { return new NotifyCollection<long>(_bindingAccountIds, RaiseOnBindingAccountIdsChanged); }
        }

        /// <summary>
        ///     Binding hashtags
        /// </summary>
        public IEnumerable<string> BindingHashtags
        {
            get { return _bindingHashtags ?? Enumerable.Empty<string>(); }
            set { _bindingHashtags = (value ?? Enumerable.Empty<string>()).ToList(); }
        }

        public TimelineModel Timeline { get; private set; }

        /// <summary>
        ///     Filter query info
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilterQuery FilterQuery
        {
            get { return _filterQuery; }
            set
            {
                if (IsActivated)
                    throw new InvalidOperationException("タブ情報がアクティブな状態のままフィルタクエリの交換を行うことはできません。");
                if (value.Equals(_filterQuery)) return;
                _filterQuery = value;
                if (_filterQuery != null)
                    _evaluator = _filterQuery.GetEvaluator();
                else
                    _evaluator = _ => false;
            }
        }

        /// <summary>
        ///     Filter querified string
        /// </summary>
        public string FilterQueryString
        {
            get { return FilterQuery.ToQuery(); }
            set
            {
                try
                {
                    FilterQuery = QueryCompiler.Compile(value);
                }
                catch (FilterQueryException fex)
                {
                    Debug.WriteLine(fex);
                    AppInformationHub.PublishInformation(new AppInformation(AppInformationKind.Warning,
                                                                            "TABINFO_QUERY_CORRUPTED_" + Name,
                                                                            "クエリが壊れています。",
                                                                            "タブ " + Name +
                                                                            " のクエリは破損しているため、フィルタが初期化されました。" +
                                                                            Environment.NewLine +
                                                                            "送出された例外: " + fex));
                }
            }
        }

        public bool IsActivated { get; private set; }

        private void InvalidateCollection()
        {
            TimelineModel oldt = Timeline;
            Timeline = new TimelineModel(_evaluator, GetChunk);
            oldt.Dispose();
        }

        /// <summary>
        ///     タブ情報をアクティベートします。
        ///     <para />
        ///     フィルタクエリをアクティベートし、タイムラインを生成し、
        ///     <para />
        ///     ストリームの読み込みを開始します。
        /// </summary>
        /// <returns></returns>
        public void Activate()
        {
            if (!IsActivated)
            {
                if (Timeline != null)
                    Timeline.Dispose();
                Timeline = new TimelineModel(_evaluator, GetChunk);
                if (FilterQuery != null)
                {
                    FilterQuery.Activate();
                    FilterQuery.OnInvalidateRequired += InvalidateCollection;
                }
            }
            IsActivated = true;
        }

        private IObservable<TwitterStatus> GetChunk(long? maxId, int chunkCount, bool isBatch)
        {
            return (isBatch ?
                StatusStore.FindBatch(_evaluator, chunkCount) :
                StatusStore.Find(_evaluator, maxId != null ? FindRange<long>.By(maxId.Value) : null, chunkCount))
                              .OrderByDescending(_ => _.CreatedAt)
                              .Take(chunkCount);
        }

        public void Deactivate()
        {
            if (IsActivated)
            {
                if (FilterQuery != null)
                {
                    FilterQuery.Deactivate();
                    FilterQuery.OnInvalidateRequired -= InvalidateCollection;
                }
                if (Timeline != null)
                {
                    Timeline.Dispose();
                    Timeline = null;
                }
            }
            IsActivated = false;
        }

        public IObservable<Unit> ReceiveTimelines(long? maxId)
        {
            return FilterQuery.Sources
                              .Select(_ => _.Receive(maxId))
                              .Merge()
                              .SelectMany(StoreHub.MergeStore)
                              .OfType<Unit>();
        }

        private class NotifyCollection<T> : ICollection<T>
        {
            private readonly ICollection<T> _source;
            private readonly Action _handler;

            public NotifyCollection(ICollection<T> source, Action handler)
            {
                Debug.Assert(source != null, "source != null");
                Debug.Assert(handler != null, "handler != null");
                _source = source;
                _handler = handler;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T item)
            {
                _source.Add(item);
                _handler();
            }

            public void Clear()
            {
                _source.Clear();
                _handler();
            }

            public bool Contains(T item)
            {
                return _source.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _source.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                return _source.Remove(item);
            }

            public int Count
            {
                get { return _source.Count; }
            }

            public bool IsReadOnly
            {
                get { return _source.IsReadOnly; }
            }
        }
    }
}