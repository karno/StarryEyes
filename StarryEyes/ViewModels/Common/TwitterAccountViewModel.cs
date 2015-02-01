using System;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;

namespace StarryEyes.ViewModels.Common
{
    public class TwitterAccountViewModel : ViewModel
    {
        private readonly TwitterAccount _account;

        public TwitterAccountViewModel(TwitterAccount account)
        {
            this._account = account;
        }

        public long Id
        {
            get { return this._account.Id; }
        }

        public TwitterAccount Account
        {
            get { return this._account; }
        }

        public Uri ProfileImageUri
        {
            get
            {
                if (this._account.UnreliableProfileImage == null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var user = await this._account.ShowUserAsync(ApiAccessProperties.Default,
                                new UserParameter(this._account.Id));
                            this._account.UnreliableProfileImage = user.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            this.RaisePropertyChanged(() => this.ProfileImageUri);
                        }
                        // ReSharper disable EmptyGeneralCatchClause
                        catch { }
                        // ReSharper restore EmptyGeneralCatchClause
                    });
                }
                return this._account.UnreliableProfileImage;
            }
        }

        public string ScreenName
        {
            get { return this._account.UnreliableScreenName; }
        }
    }
}