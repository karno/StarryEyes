using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Livet;
using StarryEyes.Annotations;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    public class AccountSelectorModel : NotificationObject
    {
        private readonly ObservableSynchronizedCollection<TwitterAccount> _accounts =
            new ObservableSynchronizedCollection<TwitterAccount>();

        private TabModel _currentFocusTab;
        private InputData _currentInputData;
        private bool _isSynchronizedWithTab;

        internal AccountSelectorModel([NotNull] InputData initialInputData)
        {
            if (initialInputData == null) throw new ArgumentNullException("initialInputData");
            _isSynchronizedWithTab = true;
            _currentInputData = initialInputData;
            Accounts.CollectionChanged += HandleCollectionChanged;
        }

        private void HandleCollectionChanged(object o, NotifyCollectionChangedEventArgs e)
        {
            if (_currentFocusTab == null || !_isSynchronizedWithTab) return;
            var newItems = e.NewItems.OfType<TwitterAccount>().Select(i => i.Id);
            var oldItems = e.OldItems.OfType<TwitterAccount>().Select(i => i.Id);
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    newItems.ForEach(_currentFocusTab.BindingAccounts.Add);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    oldItems.ForEach(i => _currentFocusTab.BindingAccounts.Remove(i));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    oldItems.ForEach(i => _currentFocusTab.BindingAccounts.Remove(i));
                    newItems.ForEach(_currentFocusTab.BindingAccounts.Add);
                    break;
                case NotifyCollectionChangedAction.Move:
                    // nop
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _currentFocusTab.BindingAccounts.Clear();
                    Accounts.Select(i => i.Id)
                             .ForEach(_currentFocusTab.BindingAccounts.Add);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsSynchronizedWithTab
        {
            get { return _isSynchronizedWithTab; }
        }

        [CanBeNull]
        public TabModel CurrentFocusTab
        {
            get { return _currentFocusTab; }
            set
            {
                if (_currentFocusTab == value) return;
                _currentFocusTab = value;
                if (_isSynchronizedWithTab)
                {
                    // propagate accounts bound with tab as selected account.
                    SynchronizeWithTab();
                }
            }
        }

        [NotNull]
        public InputData CurrentInputData
        {
            get { return _currentInputData; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _currentInputData = value;
                if (value.Accounts.Any())
                {
                    // override with explicitly specified accounts
                    SetOverride(value.Accounts);
                }
                else
                {
                    // enforced synchronizing
                    SynchronizeWithTab();
                }
            }
        }

        [NotNull]
        public ObservableSynchronizedCollection<TwitterAccount> Accounts
        {
            get { return _accounts; }
        }

        public void SetOverride([NotNull] IEnumerable<TwitterAccount> accounts)
        {
            if (accounts == null) throw new ArgumentNullException("accounts");
            _isSynchronizedWithTab = false;
            Accounts.Clear();
            accounts.ForEach(Accounts.Add);
            RaisePropertyChanged(() => IsSynchronizedWithTab);
        }

        public void SynchronizeWithTab()
        {
            _isSynchronizedWithTab = false; // suppress updating
            if (CurrentFocusTab == null)
            {
                Accounts.Clear();
            }
            else
            {
                var exists = Accounts.Select(a => a.Id)
                                      .OrderBy(a => a);
                var provided = _currentFocusTab.BindingAccounts
                                               .OrderBy(a => a);
                // if tab is not have same accounts...
                if (!exists.SequenceEqual(provided))
                {
                    Accounts.Clear();
                    Setting.Accounts
                           .Collection
                           .Where(a => _currentFocusTab.BindingAccounts.Contains(a.Id))
                           .ForEach(Accounts.Add);
                }
            }
            _isSynchronizedWithTab = true;
            CurrentInputData.Accounts = Accounts.ToArray();
            RaisePropertyChanged(() => IsSynchronizedWithTab);
        }
    }
}
