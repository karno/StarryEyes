using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Livet;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Tab
{
    public static class TabManager
    {
        private static readonly Stack<TabModel> _closedTabsStack = new Stack<TabModel>();

        private static readonly ObservableSynchronizedCollection<ColumnModel> _columns =
            new ObservableSynchronizedCollection<ColumnModel>();

        private static int _currentFocusColumnIndex;

        public static event Action OnCurrentFocusColumnChanged;

        public static ObservableSynchronizedCollection<ColumnModel> Columns
        {
            get { return _columns; }
        }

        static TabManager()
        {
            RegisterEvents();
            App.OnApplicationExit += App_OnApplicationExit;
        }

        static void App_OnApplicationExit()
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
            _loaded = true;
            var cache = LoadCache();
            if (cache != null)
            {
                Setting.Columns.Zip(cache,
                                    (col, colc) =>
                                    new ColumnModel(
                                        col.Tabs
                                           .Zip(colc,
                                                (tab, tabc) =>
                                                tab.ToTabModel(tabc.ToArray()))
                                           .ToArray()))
                       .ForEach(_columns.Add);
            }
            else
            {
                Setting.Columns
                       .Select(c => new ColumnModel(c.Tabs.Select(d => d.ToTabModel()).ToArray()))
                       .ForEach(_columns.Add);
            }

            if (_columns.Count == 0)
            {
                _columns.Add(new ColumnModel(Enumerable.Empty<TabModel>()));
            }
            App.RaiseUserInterfaceReady();
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
            SaveCache(Columns.Select(c => c.Tabs.Select(t => t.TabContentCache ?? new long[0])));
        }

        private static readonly object _cacheLock = new object();

        private static IEnumerable<IEnumerable<IEnumerable<long>>> LoadCache()
        {
            lock (_cacheLock)
            {
                try
                {
                    if (!File.Exists(App.TabTempFilePath)) return null;
                    using (var fs = File.OpenRead(App.TabTempFilePath))
                    using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
                    using (var br = new BinaryReader(ds))
                    {
                        // ReSharper disable AccessToDisposedClosure
                        return Enumerable
                            .Range(0, br.ReadInt32())
                            .Select(_ => Enumerable
                                             .Range(0, br.ReadInt32())
                                             .Select(__ => Enumerable
                                                               .Range(0, br.ReadInt32())
                                                               .Select(___ => br.ReadInt64())
                                                               .ToArray())
                                             .ToArray())
                            .ToArray();
                        // ReSharper restore AccessToDisposedClosure
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void SaveCache(IEnumerable<IEnumerable<IEnumerable<long>>> cache)
        {
            lock (_cacheLock)
            {
                try
                {
                    var array = cache.Select(ci => ci.Select(ids => ids.ToArray()).ToArray()).ToArray();
                    using (var fs = File.OpenWrite(App.TabTempFilePath))
                    using (var ds = new DeflateStream(fs, CompressionMode.Compress))
                    using (var bw = new BinaryWriter(ds))
                    {
                        bw.Write(array.Length);
                        foreach (var column in array)
                        {
                            bw.Write(column.Length);
                            foreach (var tab in column)
                            {
                                bw.Write(tab.Length);
                                foreach (var id in tab)
                                {
                                    bw.Write(id);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    BackpanelModel.RegisterEvent(new OperationFailedEvent("キャッシュの更新に失敗しました: " + ex.Message));
                    try
                    {
                        if (File.Exists(App.TabTempFilePath))
                        {
                            File.Delete(App.TabTempFilePath);
                        }
                    }
                    catch
                    {
                    }
                }
            }
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
                _currentFocusColumnIndex = value;
                var col = _columns[_currentFocusColumnIndex];
                InputAreaModel.NotifyChangeFocusingTab(col.Tabs[col.CurrentFocusTabIndex]);
                Action handler = OnCurrentFocusColumnChanged;
                if (handler != null) handler();
            }
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
        ///     Find tab info where existed.
        /// </summary>
        /// <param name="info">tab info</param>
        /// <param name="colIndex">column index</param>
        /// <param name="tabIndex">tab index</param>
        public static void GetTabInfoIndexes(TabModel info, out int colIndex, out int tabIndex)
        {
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                for (int ti = 0; ti < _columns[ci].Tabs.Count; ti++)
                {
                    if (_columns[ci].Tabs[ti] == info)
                    {
                        colIndex = ci;
                        tabIndex = ti;
                        return;
                    }
                }
            }
            throw new ArgumentException("specified TabInfo was not found.");
        }

        /// <summary>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="columnIndex"></param>
        /// <param name="tabIndex"></param>
        public static void MoveTo(TabModel info, int columnIndex, int tabIndex)
        {
            int fci, fti;
            GetTabInfoIndexes(info, out fci, out fti);
            MoveTo(fci, fti, columnIndex, tabIndex);
        }

        /// <summary>
        ///     Move specified tab.
        /// </summary>
        public static void MoveTo(int fromColumnIndex, int fromTabIndex, int destColumnIndex, int destTabIndex)
        {
            if (fromColumnIndex == destColumnIndex)
            {
                // in-column moving
                _columns[fromColumnIndex].Tabs.Move(fromTabIndex, fromColumnIndex);
            }
            else
            {
                TabModel tab = _columns[fromColumnIndex].Tabs[fromTabIndex];
                _columns[fromColumnIndex].Tabs.RemoveAt(fromTabIndex);
                _columns[destTabIndex].Tabs.Insert(destTabIndex, tab);
            }
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
        ///     Close a tab.
        /// </summary>
        public static void CloseTab(int colIndex, int tabIndex)
        {
            var ti = _columns[colIndex].Tabs[tabIndex];
            ti.Deactivate();
            _closedTabsStack.Push(ti);
            _columns[colIndex].Tabs.RemoveAt(tabIndex);
            if (_columns[colIndex].Tabs.Count == 0 && _columns.Count > 2)
            {
                CloseColumn(colIndex);
            }
            Save();
        }

        public static void CloseColumn(int colIndex)
        {
            var col = _columns[colIndex];
            col.Tabs.ForEach(ti =>
            {
                ti.Deactivate();
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
            TabModel ti = _closedTabsStack.Pop();
            CreateTab(ti);
        }

        /// <summary>
        ///     Clear closed tabs stack.
        /// </summary>
        public static void CrearClosedTabsStack()
        {
            _closedTabsStack.Clear();
        }

        public static void RegisterEvents()
        {
            MainWindowModel.OnTimelineFocusRequested += MainWindowModel_OnTimelineFocusRequested;
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("RestoreTab", ReviveTab),
                KeyAssignAction.Create("CloseTab", () =>
                {
                    var ccolumn = Columns[CurrentFocusColumnIndex];
                    if (ccolumn.Tabs.Count == 0) return;
                    CloseTab(CurrentFocusColumnIndex, ccolumn.CurrentFocusTabIndex);
                }));

        }

        static void MainWindowModel_OnTimelineFocusRequested(TimelineFocusRequest req)
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
                    ccolumn.CurrentFocusTabIndex = ltab < 0 ? ccolumn.Tabs.Count - 1 : ltab;
                    break;
                case TimelineFocusRequest.RightTab:
                    var rtab = ccolumn.CurrentFocusTabIndex + 1;
                    ccolumn.CurrentFocusTabIndex = rtab >= ccolumn.Tabs.Count ? 0 : rtab;
                    break;
            }
        }
    }
}