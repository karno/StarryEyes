using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Livet.EventListeners;
using Livet.Messaging;
using StarryEyes.Models.Timelines.SearchFlips;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.ViewModels.WindowParts;
using StarryEyes.ViewModels.WindowParts.Flips.SearchFlips;

namespace StarryEyes.ViewModels.Timelines.SearchFlips
{
    public class UserTimelineViewModel : TimelineViewModelBase
    {
        private readonly UserInfoViewModel _parent;
        private readonly UserTimelineModel _model;

        public UserTimelineViewModel(UserInfoViewModel parent, UserTimelineModel model)
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

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = TabManager.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccounts : Enumerable.Empty<long>();
            }
        }

        public override void GotFocus()
        {
            MainAreaViewModel.TimelineActionTargetOverride = this;
        }

        public void SetFocus()
        {
            this.Messenger.RaiseSafe(() => new InteractionMessage("SetFocus"));
        }

        [UsedImplicitly]
        public void PinToTab()
        {
            var sn = _parent.ScreenName;
            if (sn.StartsWith("@"))
            {
                sn = sn.Substring(1);
            }
            TabManager.CreateTab(TabModel.Create(sn,
                "from local where (user == @" + sn + " & !retweet) | retweeter == @" + sn));
            _parent.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _model.Dispose();
                MainAreaViewModel.TimelineActionTargetOverride = null;
            }
        }
    }
}
