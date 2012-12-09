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
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.WindowParts
{
    public class MainAreaViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollection<ColumnViewModel> _columns;
        public ReadOnlyDispatcherCollection<ColumnViewModel> Columns
        {
            get { return _columns; }
        }

        public MainAreaViewModel()
        {
            this.CompositeDisposable.Add(_columns = ViewModelHelper.CreateReadOnlyDispatcherCollection(TabManager.Tabs,
                _ => new ColumnViewModel(_), DispatcherHelper.UIDispatcher));
        }
    }
}
