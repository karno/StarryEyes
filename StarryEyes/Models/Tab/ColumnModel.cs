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

        public event Action CurrentFocusTabChanged;

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
                var handler = this.CurrentFocusTabChanged;
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
            CurrentFocusTabIndex = _tabs.Count - 1;
        }

        public void RemoveTab(int index)
        {
            _tabs.RemoveAt(index);
            if (this._tabs.Count <= 0) return;
            this.CurrentFocusTabIndex = index > this._tabs.Count - 1 ? index - 1 : index;
        }
    }
}
