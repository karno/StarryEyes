using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Codeplex.OAuth;
using Livet;
using Livet.Commands;
using Livet.Messaging.Windows;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.Dialogs
{
    public class AuthorizationViewModel : ViewModel
    {
        public const string RequestTokenEndpoint = "https://api.twitter.com/oauth/request_token";
        public const string AuthorizationEndpoint = "https://api.twitter.com/oauth/authorize";
        public const string AccessTokenEndpoint = "https://api.twitter.com/oauth/access_token";

        private Subject<AuthenticateInfo> returnSubject = new Subject<AuthenticateInfo>();
        public IObservable<AuthenticateInfo> AuthorizeObservable
        {
            get { return returnSubject; }
        }

        public AuthorizationViewModel()
        {
            this.CompositeDisposable.Add(() => returnSubject.OnCompleted());
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
                    this.Messenger.Raise(new TaskDialogMessage(
                        new TaskDialogInterop.TaskDialogOptions()
                        {
                            Title = "認証失敗",
                            MainIcon = TaskDialogInterop.VistaTaskDialogIcon.Error,
                            MainInstruction = "Twitterと正しく通信できませんでした。",
                            Content = "何度も繰り返し発生する場合は、しばらく時間を置いて試してみてください。",
                            CommonButtons = TaskDialogInterop.TaskDialogCommonButtons.Close,
                            FooterIcon = TaskDialogInterop.VistaTaskDialogIcon.Information,
                            FooterText = "コンピュータの時計が大幅にずれている場合も認証が行えないことがあります。"
                        }));
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
                    this.Messenger.Raise(new TaskDialogMessage(
                        new TaskDialogInterop.TaskDialogOptions()
                        {
                            Title = "アクセス許可取得失敗",
                            MainIcon = TaskDialogInterop.VistaTaskDialogIcon.Error,
                            MainInstruction = "アカウントを認証できませんでした。",
                            Content = "PINを確認しもう一度入力するか、最初からやり直してみてください。",
                            CommonButtons = TaskDialogInterop.TaskDialogCommonButtons.Close,
                            FooterIcon = TaskDialogInterop.VistaTaskDialogIcon.Information,
                            FooterText = "コンピュータの時計が大幅にずれている場合も認証が行えないことがあります。"
                        }));
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
