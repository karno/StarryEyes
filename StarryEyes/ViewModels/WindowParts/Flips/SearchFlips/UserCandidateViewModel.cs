using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Models.Stores;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserCandidateViewModel : ViewModel
    {
        private readonly string _query;
        private readonly ObservableCollection<UserViewModel> _users = new ObservableCollection<UserViewModel>();
        public ObservableCollection<UserViewModel> Users
        {
            get { return _users; }
        }

        private int _currentPageCount = -1;

        public UserCandidateViewModel(string query)
        {
            _query = query;
            LoadMore();
        }

        private void LoadMore()
        {
            var info = AccountsStore.Accounts
                                    .Shuffle()
                                    .Select(s => s.AuthenticateInfo)
                                    .FirstOrDefault();
            var page = Interlocked.Increment(ref _currentPageCount);
            info.SearchUser(_query, count: 100, page: page)
                .ObserveOnDispatcher()
                .Subscribe(u => Users.Add(new UserViewModel(u)));
        }
    }
}
