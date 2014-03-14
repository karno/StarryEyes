using System;
using System.Collections.Generic;
using System.Linq;
using Livet.EventListeners;
using Livet.Messaging;
using StarryEyes.Annotations;
using StarryEyes.Models.Timelines.SearchFlips;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.ViewModels.WindowParts;
using StarryEyes.ViewModels.WindowParts.Flips;

namespace StarryEyes.ViewModels.Timelines.SearchFlips
{
    public class SearchResultViewModel : TimelineViewModelBase
    {
        private readonly SearchFlipViewModel _parent;
        private readonly SearchResultModel _model;

        public string Query
        {
            get { return this._model.Query; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = TabManager.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccounts : Enumerable.Empty<long>();
            }
        }

        public SearchResultViewModel(SearchFlipViewModel parent, SearchResultModel model)
            : base(model)
        {
            _parent = parent;
            _model = model;
            this.CompositeDisposable.Add(
                new EventListener<Action>(
                    h => _model.FocusRequired += h,
                    h => _model.FocusRequired -= h,
                    this.SetFocus));
        }

        public void SetFocus()
        {
            this.Messenger.Raise(new InteractionMessage("SetFocus"));
        }

        public override void GotFocus()
        {
            MainAreaViewModel.TimelineActionTargetOverride = this;
        }

        [UsedImplicitly]
        public void Close()
        {
            this._parent.RewindStack();
        }

        [UsedImplicitly]
        public void PinToTab()
        {
            TabManager.CreateTab(TabModel.Create(Query, _model.CreateFilterQuery()));
            this._parent.RewindStack();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            this._model.Dispose();
            MainAreaViewModel.TimelineActionTargetOverride = null;
        }
    }
}
