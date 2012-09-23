using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Mystique.Filters;
using StarryEyes.Mystique.Filters.Parsing;
using StarryEyes.Mystique.Models.Hub;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;
using System.Reactive;
using System.Reactive.Disposables;
using Livet;

namespace StarryEyes.Mystique.Models.Tab
{
    /// <summary>
    /// Hold tab information for spawning tab.
    /// </summary>
    public class TabInfo
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

        /// <summary>
        /// Notify collection was invalidated totally.
        /// </summary>
        public event Action OnCollectionInvalidateRequired;

        public IObservable<StatusNotification> GetFilteredStream()
        {
            return StatusStore.StatusPublisher
                .Where(sn => !sn.IsAdded || evaluator(sn.Status));
        }

        public IObservable<TwitterStatus> GetChunk(long? maxId, int chunkCount)
        {
            return StatusStore.Find(evaluator,
                maxId != null ? FindRange<long>.By(maxId.Value) : null,
                chunkCount);
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
                        this._timeline = new Timeline(evaluator, GetChunk);
                        if (!_isActivated && this.filterQuery != null)
                            this.filterQuery.Activate();
                    }
                    _isActivated = true;
                });
        }

        public void Deactivate()
        {
            if (_isActivated)
            {
                if (this.filterQuery != null)
                    this.filterQuery.Deactivate();
                if (this._timeline != null)
                {
                    this._timeline.Dispose();
                    this._timeline = null;
                }
            }
            _isActivated = false;
        }

        private Timeline _timeline = null;
        public Timeline Timeline
        {
            get { return _timeline; }
        }

    }
}
