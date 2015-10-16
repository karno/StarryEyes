using System;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;

namespace StarryEyes.ViewModels.Common
{
    public class TwitterAccountViewModel : ViewModel
    {
        private readonly TwitterAccount _account;

        public TwitterAccountViewModel(TwitterAccount account)
        {
            _account = account;
        }

        public long Id
        {
            get { return _account.Id; }
        }

        public TwitterAccount Account
        {
            get { return _account; }
        }

        public Uri ProfileImageUri
        {
            get
            {
                if (_account.UnreliableProfileImage == null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var user = await _account.ShowUserAsync(_account.Id).ConfigureAwait(false);
                            _account.UnreliableProfileImage = user.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            RaisePropertyChanged(() => ProfileImageUri);
                        }
                        // ReSharper disable EmptyGeneralCatchClause
                        catch { }
                        // ReSharper restore EmptyGeneralCatchClause
                    });
                }
                return _account.UnreliableProfileImage;
            }
        }

        public string ScreenName
        {
            get { return _account.UnreliableScreenName; }
        }
    }
}