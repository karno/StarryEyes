using System;
using System.Reactive.Linq;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Operations
{
    public class FavoriteOperation : OperationBase<TwitterStatus>
    {
        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        public bool IsAddFavorite { get; set; }

        private Action cancelHandler;
        private Action completeHandler;
        public FavoriteOperation() { }
        public FavoriteOperation(AuthenticateInfo info, TwitterStatus status, bool add, Action cancel, Action completed)
        {
            AuthInfo = info;
            TargetId = status.Id;
            DescriptionText = status.ToString();
            IsAddFavorite = add;
            cancelHandler = cancel;
            completeHandler = completed;
        }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return ExecFavorite()
                .Do(_ => { if (completeHandler != null)  completeHandler(); })
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
