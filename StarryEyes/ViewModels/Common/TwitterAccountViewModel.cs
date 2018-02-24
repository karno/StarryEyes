using System;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Util;
using Livet;
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

        public long Id => _account.Id;

        public TwitterAccount Account => _account;

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
                            var user = await _account
                                .CreateAccessor().ShowUserAsync(new UserParameter(_account.Id), CancellationToken.None)
                                .ConfigureAwait(false);
                            _account.UnreliableProfileImage =
                                user.Result.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            RaisePropertyChanged(() => ProfileImageUri);
                        }
                        // ReSharper disable EmptyGeneralCatchClause
                        catch
                        {
                        }
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