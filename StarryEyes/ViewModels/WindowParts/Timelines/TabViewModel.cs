using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using StarryEyes.Models;
using StarryEyes.Models.Tab;
using System.Reactive.Linq;
using System.Reactive;
using StarryEyes.Settings;
using StarryEyes.Moon.Api.Rest;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    /// <summary>
    /// タブにバインドされるViewModelを表現します。
    /// </summary>
    public class TabViewModel : ViewModel
    {
        private TabModel _model;
        public TabModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public TabViewModel(TabModel tabModel)
        {
            this._model = tabModel;
            this._readonlyTimeline = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                tabModel.Timeline.Statuses, _ => new StatusViewModel(this, _, _model.BindingAccountIds),
                DispatcherHelper.UIDispatcher);
            this.CompositeDisposable.Add(_readonlyTimeline);
            this.CompositeDisposable.Add(Disposable.Create(() => tabModel.Deactivate()));
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

        private readonly ReadOnlyDispatcherCollection<StatusViewModel> _readonlyTimeline;
        public ReadOnlyDispatcherCollection<StatusViewModel> Timeline
        {
            get { return _readonlyTimeline; }
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
