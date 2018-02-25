using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Models.Inputting;

namespace StarryEyes.Models.Timelines.Tabs
{
    public class ColumnModel
    {
        private readonly ObservableSynchronizedCollectionEx<TabModel> _tabs =
            new ObservableSynchronizedCollectionEx<TabModel>();

        private int _currentFocusTabIndex;

        public event Action CurrentFocusTabChanged;

        public ObservableSynchronizedCollectionEx<TabModel> Tabs => _tabs;

        public int CurrentFocusTabIndex
        {
            get => _currentFocusTabIndex;
            set
            {
                if (value < 0 || value >= _tabs.Count) return;
                _currentFocusTabIndex = value;
                InputModel.AccountSelector.CurrentFocusTab = _tabs[value];
                CurrentFocusTabChanged?.Invoke();
            }
        }

        public ColumnModel()
        {
        }

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
            // commit changes
            TabManager.Save();
        }

        public void RemoveTab(int index)
        {
            if (_tabs.Count > 1)
            {
                CurrentFocusTabIndex = index >= _tabs.Count - 1 ? index - 1 : index;
            }
            _tabs.RemoveAt(index);
            // commit changes
            TabManager.Save();
        }
    }
}