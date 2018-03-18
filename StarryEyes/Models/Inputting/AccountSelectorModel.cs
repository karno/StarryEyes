using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    public class AccountSelectorModel : NotificationObject
    {
        private readonly InputCoreModel _coreModel;

        private TabModel _currentFocusTab;

        internal AccountSelectorModel([CanBeNull] InputCoreModel coreModel)
        {
            _coreModel = coreModel;
            IsSynchronizedWithTab = true;
            Accounts.CollectionChanged += HandleCollectionChanged;
        }

        private void HandleCollectionChanged(object o, NotifyCollectionChangedEventArgs e)
        {
            if (_currentFocusTab == null || !IsSynchronizedWithTab) return;
            var newItems = e.NewItems?.OfType<TwitterAccount>().Select(i => i.Id) ?? Enumerable.Empty<long>();
            var oldItems = e.OldItems?.OfType<TwitterAccount>().Select(i => i.Id) ?? Enumerable.Empty<long>();
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
            _coreModel.CurrentInputData.Accounts = Accounts.ToArray();
        }

        public bool IsSynchronizedWithTab { get; private set; }

        [CanBeNull]
        public TabModel CurrentFocusTab
        {
            get => _currentFocusTab;
            set
            {
                if (_currentFocusTab == value) return;
                // notify to coremodel
                _coreModel.ChangeFocusingTab(_currentFocusTab, value);
                _currentFocusTab = value;

                // synchronize accounts
                if (IsSynchronizedWithTab)
                {
                    // propagate accounts bound with tab as selected account.
                    SynchronizeWithTab();
                }
            }
        }

        public void CurrentInputDataChanged()
        {
            if (_coreModel.CurrentInputData.Accounts != null)
            {
                // override with explicitly specified accounts
                SetOverride(_coreModel.CurrentInputData.Accounts);
            }
            else
            {
                // enforced synchronizing
                SynchronizeWithTab();
            }
        }

        [NotNull]
        public ObservableSynchronizedCollectionEx<TwitterAccount> Accounts { get; } =
            new ObservableSynchronizedCollectionEx<TwitterAccount>();

        private void SetOverride([CanBeNull] IEnumerable<TwitterAccount> accounts)
        {
            if (accounts == null) throw new ArgumentNullException(nameof(accounts));
            IsSynchronizedWithTab = false;
            Accounts.Clear();
            accounts.ForEach(Accounts.Add);
            RaisePropertyChanged(() => IsSynchronizedWithTab);
        }

        public void SynchronizeWithTab()
        {
            IsSynchronizedWithTab = false; // suppress updating
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
                _coreModel.CurrentInputData.Accounts = Accounts.ToArray();
            }
            IsSynchronizedWithTab = true;
            RaisePropertyChanged(() => IsSynchronizedWithTab);
        }
    }
}