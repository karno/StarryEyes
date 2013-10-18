using System.Collections.Generic;
using System.Linq;
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
            this.Messenger.Raise(new InteractionMessage("SetFocus"));
        }

        public void PinToTab()
        {
            TabManager.CreateTab(TabModel.Create(
                _parent.ScreenName, "from local where user == \"" + _parent.ScreenName + "\""));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _model.Dispose();
            }
        }
    }
}
