using System.Collections.ObjectModel;
using Livet;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public abstract class UserListViewModelBase : ViewModel
    {
        private readonly ObservableCollection<UserResultItemViewModel> _users = new ObservableCollection<UserResultItemViewModel>();

        public ObservableCollection<UserResultItemViewModel> Users
        {
            get { return this._users; }
        }

        private bool _isLoading;
        private bool _isScrollInBottom;

        public bool IsLoading
        {
            get { return this._isLoading; }
            set
            {
                this._isLoading = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsScrollInBottom
        {
            get { return this._isScrollInBottom; }
            set
            {
                if (this._isScrollInBottom == value) return;
                this._isScrollInBottom = value;
                this.RaisePropertyChanged();
                if (value)
                {
                    this.ReadMore();
                }
            }
        }

        protected abstract void ReadMore();
    }
}