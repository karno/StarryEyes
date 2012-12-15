using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    /// <summary>
    /// タブにバインドされるViewModelを表現します。
    /// </summary>
    public class TabViewModel : ViewModel
    {
        private ColumnViewModel _owner;

        private TabModel _model;
        public TabModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public TabViewModel(ColumnViewModel owner, TabModel tabModel)
        {
            this._owner = owner;
            this._model = tabModel;
            if (tabModel.IsActivated)
            {
                Initialize();
            }
            else
            {
                this.IsLoading = true;
                Observable.Start(() => tabModel.Activate())
                    .SelectMany(_ => _)
                    .ObserveOnDispatcher()
                    .Subscribe(_ => { }, Initialize);
            }
        }

        private void Initialize()
        {
            this._readonlyTimeline = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                            Model.Timeline.Statuses, _ => new StatusViewModel(this, _, _model.BindingAccountIds),
                            DispatcherHelper.UIDispatcher);
            this.CompositeDisposable.Add(_readonlyTimeline);
            this.CompositeDisposable.Add(Observable.FromEvent(
                _ => Model.Timeline.OnNewStatusArrived += _,
                _ => Model.Timeline.OnNewStatusArrived -= _)
                .Subscribe(_ => UnreadCount++));
            this.CompositeDisposable.Add(() => Model.Deactivate());
            this.IsLoading = false;
        }

        public string Name
        {
            get { return _model.Name; }
            set
            {
                _model.Name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                if (value)
                {
                    UnreadCount = 0;
                }
                RaisePropertyChanged(() => IsSelected);
            }
        }

        public void Select()
        {
            _owner.SetSelected(this.Model);
        }

        private ReadOnlyDispatcherCollection<StatusViewModel> _readonlyTimeline;
        public ReadOnlyDispatcherCollection<StatusViewModel> Timeline
        {
            get { return _readonlyTimeline; }
        }

        private int _unreadCount = 0;
        public int UnreadCount
        {
            get { return _unreadCount; }
            set
            {
                if (IsSelected)
                    _unreadCount = 0;
                else
                    _unreadCount = value;
                RaisePropertyChanged(() => UnreadCount);
                RaisePropertyChanged(() => IsUnreadExisted);
            }
        }

        public bool IsUnreadExisted
        {
            get { return UnreadCount > 0; }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged(() => IsLoading);
            }
        }

        public void ReadMore()
        {
            ReadMore(_model.Timeline.Statuses.Select(_ => _.Id).Min());
        }

        public void ReadMore(long id)
        {
            this.IsSuppressTimelineAutoTrim = true;
            this.IsLoading = true;
            Model.Timeline.ReadMore(id)
                .Finally(() => this.IsLoading = false)
                .OnErrorResumeNext(Observable.Empty<Unit>())
                .Subscribe();
        }

        public void ReadMoreFromWeb(long? id)
        {
            this.IsSuppressTimelineAutoTrim = true;
            this.IsLoading = true;
            Model.ReceiveTimelines(id)
                .Finally(() => this.IsLoading = false)
                .OnErrorResumeNext(Observable.Empty<Unit>())
                .Subscribe();
        }

        #region Call by code-behind

        public bool IsSuppressTimelineAutoTrim
        {
            get { return Model.Timeline.IsSuppressTimelineTrimming; }
            set { Model.Timeline.IsSuppressTimelineTrimming = value; }
        }

        #endregion

        #region Selection Control

        public bool IsSelectedStatusExisted
        {
            get { return Timeline.Where(_ => _.IsSelected).FirstOrDefault() != null; }
        }

        public void OnSelectionUpdated()
        {
            RaisePropertyChanged(() => IsSelectedStatusExisted);
            // TODO: Impl
        }

        public void DeselectAll()
        {
            Timeline.ForEach(s => s.IsSelected = false);
        }

        #endregion
    }
}
