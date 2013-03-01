using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet;
using Livet.Messaging;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.Controls
{
    public class SearchTextBoxViewModel : ViewModel
    {
        public event Action OnPressEnter;

        private long _previousId;

        private readonly ObservableCollection<SearchCandidateViewModel> _searchCandidates
            = new ObservableCollection<SearchCandidateViewModel>();
        public ObservableCollection<SearchCandidateViewModel> SearchCandidates
        {
            get { return _searchCandidates; }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                RaisePropertyChanged();
                if (value >= 0 && value < _searchCandidates.Count)
                {
                    _searchCandidates[value].SelectThis();
                }
            }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnTextChanged(value);
                    RaisePropertyChanged();
                }
            }
        }

        private string _errorText;
        public string ErrorText
        {
            get { return _errorText; }
            set
            {
                _errorText = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => HasError);
            }
        }

        public bool HasError
        {
            get { return String.IsNullOrEmpty(_errorText); }
        }

        private async void OnTextChanged(string value)
        {
            if (value.StartsWith("?"))
            {
                IsQueryMode = true;
                try
                {
                    await Task.Run(() => QueryCompiler.Compile(value.Substring(1)));
                    ErrorText = null;
                }
                catch (FilterQueryException fex)
                {
                    ErrorText = fex.Message;
                }
            }
            else
            {
                IsQueryMode = false;
                ErrorText = null;
            }
        }

        private bool _isQueryMode;
        public bool IsQueryMode
        {
            get { return _isQueryMode; }
            set
            {
                _isQueryMode = value;
                RaisePropertyChanged();
            }
        }

        private bool _isDropDownOpen;
        public bool IsDropDownOpen
        {
            get { return _isDropDownOpen; }
            set
            {
                _isDropDownOpen = value;
                RaisePropertyChanged();
            }
        }

        private bool _isFocusedText;
        private bool _isFocusedPopup;

        public void GotFocusToText()
        {
            _isFocusedText = true;
            IsDropDownOpen = true;
            // update current binding accounts
            var ctab = MainAreaModel.CurrentFocusTab;
            long cid = 0;
            if (ctab != null && ctab.BindingAccountIds.Count == 1)
            {
                cid = ctab.BindingAccountIds.First();
            }
            if (_previousId != cid)
            {
                _previousId = cid;
                _searchCandidates.Clear();
                var aid = AccountsStore.GetAccountSetting(_previousId);
                if (aid == null) return;
                aid.AuthenticateInfo.GetSavedSearches()
                    .ObserveOnDispatcher()
                   .Subscribe(j => _searchCandidates.Add(new SearchCandidateViewModel(this, aid.AuthenticateInfo, j.id, j.query)), ex => BackpanelModel.RegisterEvent(new OperationFailedEvent(ex.Message)));
            }
        }

        public void LostFocusFromText()
        {
            _isFocusedText = false;
            LostUpdateFocus();
        }

        public void GotFocusToPopup()
        {
            _isFocusedPopup = true;
        }

        public void LostFocusFromPopup()
        {
            _isFocusedPopup = false;
            LostUpdateFocus();
        }

        public void LostUpdateFocus()
        {
            if (!_isFocusedText && !_isFocusedPopup)
            {
                IsDropDownOpen = false;
            }
        }

        public void SetFocusToText()
        {
            this.Messenger.Raise(new InteractionMessage("FocusToTextBox"));
        }

        public void SetFocusToCandidateList()
        {
            this.Messenger.Raise(new InteractionMessage("FocusToListBox"));
        }

        public void ExecuteQuery()
        {
            SetFocusToText();
            Action handler = OnPressEnter;
            if (handler != null) handler();
        }
    }

    public class SearchCandidateViewModel : ViewModel
    {
        private readonly SearchTextBoxViewModel _parent;
        private readonly AuthenticateInfo _authenticateInfo;
        private readonly long _id;
        private readonly string _query;

        public SearchCandidateViewModel(SearchTextBoxViewModel parent,
            AuthenticateInfo authenticateInfo, long id, string query)
        {
            _parent = parent;
            _authenticateInfo = authenticateInfo;
            _id = id;
            _query = query;
        }

        public AuthenticateInfo AuthenticateInfo
        {
            get { return _authenticateInfo; }
        }

        public long Id
        {
            get { return _id; }
        }

        public string Query
        {
            get { return _query; }
        }

        public void SelectThis()
        {
            _parent.Text = Query;
        }
    }
}
