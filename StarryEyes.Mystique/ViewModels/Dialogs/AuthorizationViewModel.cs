using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Codeplex.OAuth;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;
using StarryEyes.Mystique.Helpers;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.ViewModels.Dialogs
{
    public class AuthorizationViewModel : ViewModel
    {
        const string RequestTokenEndpoint = "https://api.twitter.com/oauth/request_token";
        const string AuthorizationEndpoint = "https://api.twitter.com/oauth/authorize";
        const string AccessTokenEndpoint = "https://api.twitter.com/oauth/access_token";

        private Subject<AuthenticateInfo> returnSubject = new Subject<AuthenticateInfo>();

        public IObservable<AuthenticateInfo> AuthorizeObservable
        {
            get { return returnSubject; }
        }

        public AuthorizationViewModel()
        {
            this.CompositeDisposable.Add(Disposable.Create(() => returnSubject.OnCompleted()));
        }

        private OAuthAuthorizer authorizer;
        private RequestToken currentRequestToken;

        public void Initialize()
        {
            authorizer = new OAuthAuthorizer(App.ConsumerKey, App.ConsumerSecret);
            CurrentAuthenticationStep = AuthenticationStep.RequestingToken;
            Observable.Defer(() => authorizer.GetRequestToken(RequestTokenEndpoint))
                .Retry(3, TimeSpan.FromSeconds(3)) // twitter sometimes returns an error without any troubles.
                .Subscribe(t =>
                {
                    currentRequestToken = t.Token;
                    CurrentAuthenticationStep = AuthenticationStep.WaitingPinInput;
                    BrowserHelper.Open(authorizer.BuildAuthorizeUrl(AuthorizationEndpoint, t.Token));
                },
                ex =>
                {
                    this.Messenger.Raise(new InformationMessage(
                        "Twitterとの通信が正しくできませんでした。" + Environment.NewLine +
                        "しつこく何度も試してみるのも良いと思いますが、こういう場合は" + Environment.NewLine +
                        "すこし時間を置いて試していただくほうがよろしいかと思います。" + Environment.NewLine +
                        "(PCの時計が大幅にずれている時も認証ができないことがあります。)",
                        "認証失敗", System.Windows.MessageBoxImage.Error, null));
                    this.Messenger.Raise(new WindowActionMessage(null, WindowAction.Close));
                });
        }

        private AuthenticationStep currentAuthenticationStep = AuthenticationStep.RequestingToken;
        public AuthenticationStep CurrentAuthenticationStep
        {
            get { return currentAuthenticationStep; }
            set
            {
                currentAuthenticationStep = value;
                RaisePropertyChanged(() => CurrentAuthenticationStep);
                RaisePropertyChanged(() => IsNegotiating);
                VerifyPinCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNegotiating
        {
            get
            {
                return CurrentAuthenticationStep == AuthenticationStep.RequestingToken ||
                    CurrentAuthenticationStep == AuthenticationStep.AuthorizingUser;
            }
        }

        private string pin = String.Empty;
        public string Pin
        {
            get { return pin; }
            set
            {
                pin = value;
                RaisePropertyChanged(() => Pin);
            }
        }

        #region VerifyPinCommand
        private ViewModelCommand _VerifyPinCommand;

        public ViewModelCommand VerifyPinCommand
        {
            get
            {
                if (_VerifyPinCommand == null)
                {
                    _VerifyPinCommand = new ViewModelCommand(VerifyPin, CanVerifyPin);
                }
                return _VerifyPinCommand;
            }
        }

        public bool CanVerifyPin()
        {
            return CurrentAuthenticationStep == AuthenticationStep.WaitingPinInput;
        }

        public void VerifyPin()
        {
            CurrentAuthenticationStep = AuthenticationStep.AuthorizingUser;
            Observable.Defer(() => authorizer.GetAccessToken(AccessTokenEndpoint, currentRequestToken, Pin))
                .Retry(3, TimeSpan.FromSeconds(3))
                .Subscribe(r =>
                {
                    var id = long.Parse(r.ExtraData["user_id"].First());
                    var sn = r.ExtraData["screen_name"].First();
                    returnSubject.OnNext(new AuthenticateInfo(id, sn, r.Token));
                    this.Messenger.Raise(new WindowActionMessage(null, WindowAction.Close));
                },
                ex =>
                {
                    CurrentAuthenticationStep = AuthenticationStep.WaitingPinInput;
                    this.Messenger.Raise(new InformationMessage(
                        "Twitterからアクセス許可をもらえませんでした。" + Environment.NewLine +
                        "もう一度お試しいただくか、再度暗証番号を取得し直してください。" + Environment.NewLine +
                        "(PCの時計が大幅にずれている時も認証ができないことがあります。)",
                        "アクセス許可取得失敗", System.Windows.MessageBoxImage.Error, null));
                });
        }
        #endregion
    }

    public enum AuthenticationStep
    {
        RequestingToken,
        WaitingPinInput,
        AuthorizingUser,
        AuthorizationCompleted,
    }
}
