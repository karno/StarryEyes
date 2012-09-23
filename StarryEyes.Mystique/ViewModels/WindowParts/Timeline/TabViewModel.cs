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

using StarryEyes.Mystique.Models;
using StarryEyes.Mystique.Models.Tab;

namespace StarryEyes.Mystique.ViewModels.WindowParts.Timeline
{
    /// <summary>
    /// タブにバインドされるViewModelを表現します。
    /// </summary>
    public class TabViewModel : ViewModel
    {
        private TabInfo tabInfo;
        public TabInfo TabInfo
        {
            get { return tabInfo; }
            set { tabInfo = value; }
        }

        public TabViewModel(TabInfo tabInfo)
        {
            this.tabInfo = tabInfo;
        }

        public string Name
        {
            get { return tabInfo.Name; }
            set
            {
                tabInfo.Name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        private object _timelineLocker = new object();
        private DispatcherCollection<StatusViewModel> _timeline
            = new DispatcherCollection<StatusViewModel>(DispatcherHelper.UIDispatcher);
        public DispatcherCollection<StatusViewModel> Timeline
        {
            get { return _timeline; }
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

        #region ReadMoreCommand
        private ViewModelCommand _ReadMoreCommand;

        public ViewModelCommand ReadMoreCommand
        {
            get
            {
                if (_ReadMoreCommand == null)
                {
                    _ReadMoreCommand = new ViewModelCommand(ReadMore);
                }
                return _ReadMoreCommand;
            }
        }

        public void ReadMore()
        {
            this.IsSuppressTimelineAutoTrim = true;
            this.IsLoading = true;

        }
        #endregion

        #region Call by code-behind

        public bool IsSuppressTimelineAutoTrim { get; set; }

        #endregion
    }
}
