using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timelines.Tabs
{
    public static class TabManager
    {
        private static readonly Stack<TabModel> _closedTabsStack = new Stack<TabModel>();

        private static readonly ObservableSynchronizedCollection<ColumnModel> _columns =
            new ObservableSynchronizedCollection<ColumnModel>();

        private static int _currentFocusColumnIndex;

        public static bool CanReviveTab
        {
            get { return _closedTabsStack.Count > 0; }
        }

        public static event Action CurrentFocusColumnChanged;

        public static ObservableSynchronizedCollection<ColumnModel> Columns
        {
            get { return _columns; }
        }

        static TabManager()
        {
            RegisterEvents();
            App.ApplicationExit += AppApplicationExit;
        }

        static void AppApplicationExit()
        {
            // save cache
            Save();
        }

        private static bool _loaded;

        /// <summary>
        /// Load from configuration.
        /// </summary>
        internal static void Load()
        {
            Setting.Columns
                   .Select(c => new ColumnModel(c.Tabs.Select(d => d.ToTabModel()).ToArray()))
                   .ForEach(_columns.Add);
            GCColumn();
            if (_loaded) return;
            _loaded = true;
        }

        /// <summary>
        /// Save tab info to configuration file.
        /// </summary>
        public static void Save()
        {
            // save before load cause tab information corruption.
            if (!_loaded) return;
            Setting.Columns = Columns.Select(c => c.Tabs.Select(t => new TabDescription(t)))
                                     .Select(ts => new ColumnDescription { Tabs = ts.ToArray() }).ToArray();
        }

        /// <summary>
        /// Get current focusing tab model.
        /// </summary>
        public static TabModel CurrentFocusTab
        {
            get
            {
                if (_currentFocusColumnIndex < 0 || _currentFocusColumnIndex >= _columns.Count)
                    return null;
                var col = _columns[_currentFocusColumnIndex];
                var cti = col.CurrentFocusTabIndex;
                if (cti < 0 || cti >= col.Tabs.Count)
                    return null;
                return col.Tabs[cti];
            }
        }

        /// <summary>
        ///     Current focused column index
        /// </summary>
        public static int CurrentFocusColumnIndex
        {
            get { return _currentFocusColumnIndex; }
            set
            {
                if (_currentFocusColumnIndex == value) return;
                _currentFocusColumnIndex = value;
                RaiseCurrentFocusColumnChanged();
            }
        }

        private static void RaiseCurrentFocusColumnChanged()
        {
            var col = _columns[_currentFocusColumnIndex];
            InputAreaModel.NotifyChangeFocusingTab(col.Tabs[col.CurrentFocusTabIndex]);
            var handler = CurrentFocusColumnChanged;
            if (handler != null) handler();
        }

        /// <summary>
        ///     Check revivable tab is existed in closed tabs stack.
        /// </summary>
        public static bool IsRevivableTabExsted
        {
            get { return _closedTabsStack.Count > 0; }
        }

        /// <summary>
        ///     Get column info datas for persistence.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<ColumnInfo> GetColumnInfoData()
        {
            return _columns.Select(c => new ColumnInfo(c.Tabs));
        }

        /// <summary>
        ///     Find column info where existed.
        /// </summary>
        /// <param name="column">find column</param>
        /// <returns>If found, returns >= 0. Otherwise, return -1.</returns>
        public static int FindColumnIndex(ColumnModel column)
        {
            for (var ci = 0; ci < _columns.Count; ci++)
            {
                if (_columns[ci] == column)
                {
                    return ci;
                }
            }
            return -1;
        }

        /// <summary>
        ///     Find tab info where existed.
        /// </summary>
        /// <param name="info">tab info</param>
        /// <param name="colIndex">column index</param>
        public static int FindTabIndex(TabModel info, int colIndex)
        {
            for (var ti = 0; ti < _columns[colIndex].Tabs.Count; ti++)
            {
                if (_columns[colIndex].Tabs[ti] == info)
                {
                    return ti;
                }
            }
            return -1;
        }

        /// <summary>
        ///     Find tab info where existed.
        /// </summary>
        /// <param name="info">tab info</param>
        /// <param name="colIndex">column index</param>
        /// <param name="tabIndex">tab index</param>
        public static bool FindColumnTabIndex(TabModel info, out int colIndex, out int tabIndex)
        {
            for (var ci = 0; ci < _columns.Count; ci++)
            {
                for (var ti = 0; ti < _columns[ci].Tabs.Count; ti++)
                {
                    if (_columns[ci].Tabs[ti] != info) continue;
                    colIndex = ci;
                    tabIndex = ti;
                    return true;
                }
            }
            colIndex = -1;
            tabIndex = -1;
            return false;
        }

        /// <summary>
        ///     Move specified tab.
        /// </summary>
        public static void MoveTo(int fromColumnIndex, int fromTabIndex, int destColumnIndex, int destTabIndex)
        {
            if (fromColumnIndex == destColumnIndex)
            {

                // in-column moving
                _columns[fromColumnIndex].Tabs.Move(fromTabIndex, destTabIndex);
            }
            else
            {
                var tab = _columns[fromColumnIndex].Tabs[fromTabIndex];
                _columns[fromColumnIndex].Tabs.RemoveAt(fromTabIndex);
                _columns[destColumnIndex].Tabs.Insert(destTabIndex, tab);
            }
            GCColumn();
            Save();
        }

        /// <summary>
        ///     Create tab
        /// </summary>
        /// <param name="info">tab information</param>
        public static void CreateTab(TabModel info)
        {
            CreateTab(info, _currentFocusColumnIndex);
            Save();
        }

        /// <summary>
        ///     Create tab into specified column
        /// </summary>
        public static void CreateTab(TabModel info, int columnIndex)
        {
            // ReSharper disable LocalizableElement
            if (columnIndex > _columns.Count) // column index is only for existed or new column
                throw new ArgumentOutOfRangeException(
                    "columnIndex",
                    "currently " + _columns.Count +
                    " columns are existed. so, you can't set this parameter as " +
                    columnIndex + ".");
            // ReSharper restore LocalizableElement
            if (columnIndex == _columns.Count)
            {
                // create new
                CreateColumn(info);
            }
            else
            {
                _columns[columnIndex].CreateTab(info);
                Save();
            }
        }

        /// <summary>
        ///     Create column
        /// </summary>
        /// <param name="info">initial created tab</param>
        public static void CreateColumn(params TabModel[] info)
        {
            _columns.Add(new ColumnModel(info));
            CurrentFocusColumnIndex = _columns.Count - 1;
            Save();
        }

        /// <summary>
        ///     Create column
        /// </summary>
        /// <param name="index">insertion index</param>
        /// <param name="info">initial created tab</param>
        public static void CreateColumn(int index, params TabModel[] info)
        {
            _columns.Insert(index, new ColumnModel(info));
            CurrentFocusColumnIndex = index;
            Save();
        }

        /// <summary>
        ///     Close a tab.
        /// </summary>
        public static void CloseTab(int colIndex, int tabIndex)
        {
            var ti = _columns[colIndex].Tabs[tabIndex];
            ti.IsActivated = false;
            _closedTabsStack.Push(ti);
            _columns[colIndex].RemoveTab(tabIndex);
            GCColumn();
            Save();
        }

        /// <summary>
        /// Close column
        /// </summary>
        /// <param name="colIndex">column index</param>
        public static void CloseColumn(int colIndex)
        {
            var col = _columns[colIndex];
            col.Tabs.ForEach(ti =>
            {
                ti.IsActivated = false;
                _closedTabsStack.Push(ti);
            });
            _columns.RemoveAt(colIndex);
            Save();
        }

        /// <summary>
        ///     Revive tab from closed tabs stack.
        /// </summary>
        public static void ReviveTab()
        {
            var ti = _closedTabsStack.Pop();
            CreateTab(ti);
        }

        /// <summary>
        ///     Clear closed tabs stack.
        /// </summary>
        public static void CrearClosedTabsStack()
        {
            _closedTabsStack.Clear();
        }

        /// <summary>
        /// Register key assign events.
        /// </summary>
        public static void RegisterEvents()
        {
            MainWindowModel.TimelineFocusRequested += MainWindowModelTimelineFocusRequested;
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("RestoreTab", ReviveTab),
                KeyAssignAction.Create("CloseTab", () =>
                {
                    var ccolumn = Columns[CurrentFocusColumnIndex];
                    if (ccolumn.Tabs.Count == 0) return;
                    CloseTab(CurrentFocusColumnIndex, ccolumn.CurrentFocusTabIndex);
                }));

        }

        /// <summary>
        /// Clean-up empty columns.
        /// </summary>
        public static void GCColumn()
        {
            Columns.Select((c, i) => new { Column = c, Index = i })
                .Where(t => t.Column.Tabs.Count == 0)
                .Select(t => t.Index)
                .OrderByDescending(i => i)
                .ForEach(CloseColumn);
            if (_columns.Count == 0)
            {
                _columns.Add(new ColumnModel(Enumerable.Empty<TabModel>()));
            }
            RaiseCurrentFocusColumnChanged();
        }

        private static void MainWindowModelTimelineFocusRequested(TimelineFocusRequest req)
        {
            var ccolumn = Columns[CurrentFocusColumnIndex];
            if (ccolumn.Tabs.Count == 0) return; // not available
            switch (req)
            {
                case TimelineFocusRequest.LeftColumn:
                    var left = CurrentFocusColumnIndex - 1;
                    CurrentFocusColumnIndex = left < 0 ? Columns.Count - 1 : left;
                    break;
                case TimelineFocusRequest.RightColumn:
                    var right = CurrentFocusColumnIndex + 1;
                    CurrentFocusColumnIndex = right >= Columns.Count ? 0 : right;
                    break;
                case TimelineFocusRequest.LeftTab:
                    var ltab = ccolumn.CurrentFocusTabIndex - 1;
                    if (ltab < 0)
                    {
                        // move left column
                        var lcol = CurrentFocusColumnIndex - 1;
                        CurrentFocusColumnIndex = lcol < 0 ? Columns.Count - 1 : lcol;
                        ccolumn = Columns[CurrentFocusColumnIndex];
                        ltab = ccolumn.Tabs.Count - 1;
                    }
                    ccolumn.CurrentFocusTabIndex = ltab;
                    break;
                case TimelineFocusRequest.RightTab:
                    var rtab = ccolumn.CurrentFocusTabIndex + 1;
                    if (rtab >= ccolumn.Tabs.Count)
                    {
                        var rcol = CurrentFocusColumnIndex + 1;
                        CurrentFocusColumnIndex = rcol >= Columns.Count ? 0 : rcol;
                        ccolumn = Columns[CurrentFocusColumnIndex];
                        rtab = 0;
                    }
                    ccolumn.CurrentFocusTabIndex = rtab;
                    break;
            }
        }
    }
}