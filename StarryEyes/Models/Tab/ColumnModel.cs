using System;
using System.Collections.Generic;
using System.Linq;
using Livet;

namespace StarryEyes.Models.Tab
{
    public class ColumnModel
    {
        private readonly ObservableSynchronizedCollection<TabModel> _tabs =
            new ObservableSynchronizedCollection<TabModel>();

        private int _currentFocusTabIndex;

        public event Action OnCurrentFocusColumnChanged;

        public ObservableSynchronizedCollection<TabModel> Tabs
        {
            get { return _tabs; }
        }

        public int CurrentFocusTabIndex
        {
            get { return _currentFocusTabIndex; }
            set
            {
                _currentFocusTabIndex = value;
                InputAreaModel.NotifyChangeFocusingTab(_tabs[value]);
                Action handler = OnCurrentFocusColumnChanged;
                if (handler != null) handler();
            }
        }

        public ColumnModel() { }
        public ColumnModel(params TabModel[] tabModels)
            : this(tabModels.AsEnumerable())
        {
        }

        public ColumnModel(IEnumerable<TabModel> tabModels)
        {
            tabModels.ForEach(_tabs.Add);
        }

        public void CreateTab(TabModel info)
        {
            _tabs.Add(info);
        }
    }
}
