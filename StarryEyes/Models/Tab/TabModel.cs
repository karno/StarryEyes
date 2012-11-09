using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Hub;
using StarryEyes.Models.Store;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Vanille.DataStore;
using System.Reactive;
using System.Reactive.Disposables;
using Livet;
using System.Linq;
using StarryEyes.Breezy.Authorize;
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

        private readonly AVLTree<long> bindingAccountIds = new AVLTree<long>();
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
        public IEnumerable<string> BindingHashtags
        {
            get { return bindingHashtags ?? Enumerable.Empty<string>(); }
            set { bindingHashtags = (value ?? Enumerable.Empty<string>()).ToList(); }
        }

        public TimelineModel Timeline { get; private set; }

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
                if (IsActivated)
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
                    AppInformationHub.PublishInformation(new AppInformation(AppInformationKind.Warning,
                        "TABINFO_QUERY_CORRUPTED_" + Name,
                        "クエリが壊れています。",
                        "タブ " + Name + " のクエリは破損しているため、フィルタが初期化されました。" + Environment.NewLine +
                        "送出された例外: " + fex.ToString()));
                }
            }
        }

        private void InvalidateCollection()
        {
            var oldt = this.Timeline;
            Timeline = new TimelineModel(evaluator, GetChunk);
            oldt.Dispose();
        }

        public bool IsActivated { get; private set; }

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
                    if (!IsActivated)
                    {
                        if (this.filterQuery != null)
                            this.filterQuery.Activate();
                        if (this.Timeline != null)
                            this.Timeline.Dispose();
                        this.Timeline = new TimelineModel(evaluator, GetChunk);
                        if (this.filterQuery != null)
                        {
                            this.filterQuery.Activate();
                            this.filterQuery.OnInvalidateRequired += InvalidateCollection;
                        }
                    }
                    IsActivated = true;
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
            if (IsActivated)
            {
                if (this.filterQuery != null)
                {
                    this.filterQuery.Deactivate();
                    this.filterQuery.OnInvalidateRequired += InvalidateCollection;
                }
                if (this.Timeline != null)
                {
                    this.Timeline.Dispose();
                    this.Timeline = null;
                }
            }
            IsActivated = false;
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
