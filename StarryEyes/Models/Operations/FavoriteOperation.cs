using System;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Operations
{
    public class FavoriteOperation : OperationBase<TwitterStatus>
    {
        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        public bool IsAddFavorite { get; set; }

        public FavoriteOperation() { }

        public FavoriteOperation(AuthenticateInfo info, TwitterStatus status, bool add)
        {
            AuthInfo = info;
            TargetId = status.Id;
            DescriptionText = status.ToString();
            IsAddFavorite = add;
        }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return ExecFavorite()
                .Catch((Exception ex) =>
                {
                    return GetExceptionDetail(ex)
                        .SelectMany(s =>
                        {
                            if (s.Contains("You have already favorited this status."))
                            {
                                // favorited
                                return Observable.Empty<TwitterStatus>();
                            }
                            else
                            {
                                throw ex;
                            }
                        });
                });
        }

        private IObservable<TwitterStatus> ExecFavorite()
        {
            if (IsAddFavorite)
                return AuthInfo.CreateFavorite(TargetId);
            else
                return AuthInfo.DestroyFavorite(TargetId);
        }
    }
}
