using System;
using System.Collections.Generic;
using System.Linq;
using Livet;

namespace StarryEyes.Models.Tab
{
    public static class TabManager
    {
        private static readonly Stack<TabModel> _closedTabsStack = new Stack<TabModel>();

        private static readonly ObservableSynchronizedCollection<ObservableSynchronizedCollection<TabModel>> _tabs =
            new ObservableSynchronizedCollection<ObservableSynchronizedCollection<TabModel>>();

        private static int _currentFocusColumn;

        internal static ObservableSynchronizedCollection<ObservableSynchronizedCollection<TabModel>> Tabs
        {
            get { return _tabs; }
        }

        /// <summary>
        ///     Current focused column index
        /// </summary>
        public static int CurrentFocusColumn
        {
            get { return _currentFocusColumn; }
            set { _currentFocusColumn = value; }
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
            return _tabs.Select(t => new ColumnInfo(t));
        }

        /// <summary>
        ///     Find tab info where existed.
        /// </summary>
        /// <param name="info">tab info</param>
        /// <param name="colIndex">column index</param>
        /// <param name="tabIndex">tab index</param>
        public static void GetTabInfoIndexes(TabModel info, out int colIndex, out int tabIndex)
        {
            for (int ci = 0; ci < _tabs.Count; ci++)
            {
                for (int ti = 0; ti < _tabs[ci].Count; ti++)
                {
                    if (_tabs[ci][ti] == info)
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
                _tabs[fromColumnIndex].Move(fromTabIndex, destTabIndex);
            }
            else
            {
                TabModel tab = _tabs[fromColumnIndex][fromTabIndex];
                _tabs[fromColumnIndex].RemoveAt(fromTabIndex);
                _tabs[destColumnIndex].Insert(destTabIndex, tab);
            }
        }

        /// <summary>
        ///     Create tab
        /// </summary>
        /// <param name="info">tab information</param>
        public static void CreateTab(TabModel info)
        {
            CreateTab(info, _currentFocusColumn);
        }

        /// <summary>
        ///     Create tab into specified column
        /// </summary>
        public static void CreateTab(TabModel info, int columnIndex)
        {
            if (columnIndex > _tabs.Count) // column index is only for existed or new column
                throw new ArgumentOutOfRangeException("columnIndex",
                                                      "currently " + _tabs.Count +
                                                      " columns are created. so, you can't set this parameter as " +
                                                      columnIndex + ".");
            if (columnIndex == _tabs.Count)
            {
                // create new
                CreateColumn(info);
            }
            else
            {
                _tabs[columnIndex].Add(info);
            }
        }

        /// <summary>
        ///     Create column
        /// </summary>
        /// <param name="info">initial created tab</param>
        public static void CreateColumn(TabModel info)
        {
            _tabs.Add(new ObservableSynchronizedCollection<TabModel>(new[] { info }));
        }

        /// <summary>
        ///     Close a tab.
        /// </summary>
        public static void CloseTab(int colIndex, int tabIndex)
        {
            TabModel ti = _tabs[colIndex][tabIndex];
            ti.Deactivate();
            _closedTabsStack.Push(ti);
            _tabs[colIndex].RemoveAt(tabIndex);
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
    }
}