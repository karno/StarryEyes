using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Hub;
using StarryEyes.Models.Store;
using StarryEyes.Moon.DataModel;
using StarryEyes.Vanille.DataStore;
using System.Reactive;
using System.Reactive.Disposables;
using Livet;
using System.Linq;
using StarryEyes.Moon.Authorize;
using StarryEyes.Filters.Sources;

namespace StarryEyes.Models.Tab
{
    /// <summary>
    /// Hold tab information for spawning tab.
    /// </summary>
    public class TabModel
    {
        /// <summary>
        /// Name of this tab.
        /// </summary>
        public string Name { get; set; }

        private AVLTree<long> bindingAccountIds = new AVLTree<long>();
        /// <summary>
        /// Binding accounts ids
        /// </summary>
        public ICollection<long> BindingAccountIds
        {
            get { return bindingAccountIds; }
        }

        private List<string> bindingHashtags = new List<string>();
        /// <summary>
        /// Binding hashtags
        /// </summary>
        public List<string> BindingHashtags
        {
            get { return bindingHashtags; }
            set { bindingHashtags = value; }
        }

        private TimelineModel _timeline = null;
        public TimelineModel Timeline
        {
            get { return _timeline; }
        }

        private Func<TwitterStatus, bool> evaluator = _ => false;
        private FilterQuery filterQuery = null;
        /// <summary>
        /// Filter query info
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilterQuery FilterQuery
        {
            get { return filterQuery; }
            set
            {
                if (_isActivated)
                    throw new InvalidOperationException("タブ情報がアクティブな状態のままフィルタクエリの交換を行うことはできません。");
                if (this.filterQuery != value)
                {
                    filterQuery = value;
                    if (filterQuery != null)
                        evaluator = filterQuery.GetEvaluator();
                    else
                        evaluator = _ => false;
                }
            }
        }

        /// <summary>
        /// Filter querified string
        /// </summary>
        public string FilterQueryString
        {
            get { return filterQuery.ToQuery(); }
            set
            {
                try
                {
                    filterQuery = QueryCompiler.Compile(value);
                }
                catch (FilterQueryException fex)
                {
                    InformationHub.PublishInformation(new Information(InformationKind.Warning,
                        "TABINFO_QUERY_CORRUPTED_" + Name,
                        "クエリが壊れています。",
                        "タブ " + Name + " のクエリは破損しているため、フィルタが初期化されました。" + Environment.NewLine +
                        "送出された例外: " + fex.ToString()));
                }
            }
        }

        private void InvalidateCollection()
        {
            var oldt = this._timeline;
            _timeline = new TimelineModel(evaluator, GetChunk);
            oldt.Dispose();
        }

        private bool _isActivated = false;
        public bool IsActivated
        {
            get { return _isActivated; }
        }

        /// <summary>
        /// タブ情報をアクティベートします。<para />
        /// フィルタクエリをアクティベートし、タイムラインを生成し、<para />
        /// ストリームの読み込みを開始します。
        /// </summary>
        /// <returns></returns>
        public IObservable<Unit> Activate()
        {
            return Observable.Defer(() => Observable.Return(new Unit()))
                .Do(_ =>
                {
                    if (!_isActivated)
                    {
                        if (this.filterQuery != null)
                            this.filterQuery.Activate();
                        if (this._timeline != null)
                            this._timeline.Dispose();
                        this._timeline = new TimelineModel(evaluator, GetChunk);
                        if (this.filterQuery != null)
                        {
                            this.filterQuery.Activate();
                            this.filterQuery.OnInvalidateRequired += InvalidateCollection;
                        }
                    }
                    _isActivated = true;
                });
        }

        private IObservable<TwitterStatus> GetChunk(long? maxId, int chunkCount)
        {
            return StatusStore.Find(evaluator,
                maxId != null ? FindRange<long>.By(maxId.Value) : null,
                chunkCount);
        }

        public void Deactivate()
        {
            if (_isActivated)
            {
                if (this.filterQuery != null)
                {
                    this.filterQuery.Deactivate();
                    this.filterQuery.OnInvalidateRequired += InvalidateCollection;
                }
                if (this._timeline != null)
                {
                    this._timeline.Dispose();
                    this._timeline = null;
                }
            }
            _isActivated = false;
        }

        public IObservable<Unit> ReceiveTimelines(long? maxId)
        {
            return FilterQuery.Sources
                .ToObservable()
                .SelectMany(_ => _.Receive(maxId))
                .Do(_ => StatusStore.Store(_))
                .OfType<Unit>();
        }
    }
}
