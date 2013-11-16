using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Albireo;

namespace StarryEyes.Models.Timelines.Tabs
{
    public class ColumnModel
    {
        private readonly ObservableSynchronizedCollection<TabModel> _tabs =
            new ObservableSynchronizedCollection<TabModel>();

        private int _currentFocusTabIndex;

        public event Action CurrentFocusTabChanged;

        public ObservableSynchronizedCollection<TabModel> Tabs
        {
            get { return this._tabs; }
        }

        public int CurrentFocusTabIndex
        {
            get { return this._currentFocusTabIndex; }
            set
            {
                if (value < 0 || value >= this._tabs.Count) return;
                this._currentFocusTabIndex = value;
                InputAreaModel.NotifyChangeFocusingTab(this._tabs[value]);
                CurrentFocusTabChanged.SafeInvoke();
            }
        }

        public ColumnModel() { }
        public ColumnModel(params TabModel[] tabModels)
            : this(tabModels.AsEnumerable())
        {
        }

        public ColumnModel(IEnumerable<TabModel> tabModels)
        {
            tabModels.ForEach(this._tabs.Add);
        }

        public void CreateTab(TabModel info)
        {
            this._tabs.Add(info);
            this.CurrentFocusTabIndex = this._tabs.Count - 1;
            // commit changes
            TabManager.Save();
        }

        public void RemoveTab(int index)
        {
            if (this._tabs.Count > 1)
            {
                this.CurrentFocusTabIndex = index >= this._tabs.Count - 1 ? index - 1 : index;
            }
            this._tabs.RemoveAt(index);
            // commit changes
            TabManager.Save();
        }
    }
}
