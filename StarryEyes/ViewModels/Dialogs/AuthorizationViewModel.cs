using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using AsyncOAuth;
using Livet;
using Livet.Commands;
using Livet.Messaging.Windows;
using StarryEyes.Models.Accounting;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.Dialogs
{
    public class AuthorizationViewModel : ViewModel
    {
        public const string RequestTokenEndpoint = "https://api.twitter.com/oauth/request_token";
        public const string AuthorizationEndpoint = "https://api.twitter.com/oauth/authorize";
        public const string AccessTokenEndpoint = "https://api.twitter.com/oauth/access_token";

        private readonly Subject<TwitterAccount> _returnSubject = new Subject<TwitterAccount>();
        public IObservable<TwitterAccount> AuthorizeObservable
        {
            get { return _returnSubject; }
        }

        public AuthorizationViewModel()
        {
            this.CompositeDisposable.Add(() => _returnSubject.OnCompleted());
        }

        private OAuthAuthorizer _authorizer;
        private RequestToken _currentRequestToken;

        public void Initialize()
        {
            _authorizer = new OAuthAuthorizer(Setting.GlobalConsumerKey.Value ?? App.ConsumerKey,
             Setting.GlobalConsumerSecret.Value ?? App.ConsumerSecret);
            CurrentAuthenticationStep = AuthenticationStep.RequestingToken;
            Observable.Defer(() => _authorizer.GetRequestToken(RequestTokenEndpoint).ToObservable())
                .Retry(3, TimeSpan.FromSeconds(3)) // twitter sometimes returns an error without any troubles.
                .Subscribe(t =>
                {
                    _currentRequestToken = t.Token;
                    CurrentAuthenticationStep = AuthenticationStep.WaitingPinInput;
                    BrowserHelper.Open(_authorizer.BuildAuthorizeUrl(AuthorizationEndpoint, t.Token));
                },
                ex => this.Messenger.Raise(new TaskDialogMessage(
                                               new TaskDialogOptions
                                               {
                                                   Title = "OAuth認証エラー",
                                                   MainIcon = VistaTaskDialogIcon.Error,
                                                   MainInstruction = "Twitterと正しく通信できませんでした。",
                                                   Content = "何度も繰り返し発生する場合は、しばらく時間を置いて試してみてください。",
                                                   CommonButtons = TaskDialogCommonButtons.Close,
                                                   FooterIcon = VistaTaskDialogIcon.Information,
                                                   FooterText = "コンピュータの時計が大幅にずれている場合も認証が行えないことがあります。"
                                               })));
        }

        private AuthenticationStep _currentAuthenticationStep = AuthenticationStep.RequestingToken;
        public AuthenticationStep CurrentAuthenticationStep
        {
            get { return _currentAuthenticationStep; }
            set
            {
                _currentAuthenticationStep = value;
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

        private string _pin = String.Empty;
        public string Pin
        {
            get { return _pin; }
            set
            {
                _pin = value;
                RaisePropertyChanged(() => Pin);
            }
        }

        #region VerifyPinCommand
        private ViewModelCommand _verifyPinCommand;

        public ViewModelCommand VerifyPinCommand
        {
            get { return _verifyPinCommand ?? (_verifyPinCommand = new ViewModelCommand(VerifyPin, CanVerifyPin)); }
        }

        public bool CanVerifyPin()
        {
            return CurrentAuthenticationStep == AuthenticationStep.WaitingPinInput;
        }

        public void VerifyPin()
        {
            CurrentAuthenticationStep = AuthenticationStep.AuthorizingUser;
            Observable.Defer(() => _authorizer.GetAccessToken(AccessTokenEndpoint, _currentRequestToken, Pin).ToObservable())
                .Retry(3, TimeSpan.FromSeconds(3))
                .Subscribe(r =>
                {
                    var id = long.Parse(r.ExtraData["user_id"].First());
                    var sn = r.ExtraData["screen_name"].First();
                    _returnSubject.OnNext(new TwitterAccount(id, sn, r.Token));
                    this.Messenger.Raise(new WindowActionMessage(WindowAction.Close));
                },
                ex =>
                {
                    CurrentAuthenticationStep = AuthenticationStep.WaitingPinInput;
                    this.Messenger.Raise(new TaskDialogMessage(
                        new TaskDialogOptions
                        {
                            Title = "アクセス許可取得失敗",
                            MainIcon = VistaTaskDialogIcon.Error,
                            MainInstruction = "アカウントを認証できませんでした。",
                            Content = "PINを確認しもう一度入力するか、最初からやり直してみてください。",
                            CommonButtons = TaskDialogCommonButtons.Close,
                            FooterIcon = VistaTaskDialogIcon.Information,
                            FooterText = "コンピュータの時計が大幅にずれている場合も認証が行えないことがあります。"
                        }));
                });
        }
        #endregion

        #region Text box control

        public void OnEnterKeyDown()
        {
            if (CanVerifyPin())
            {
                VerifyPin();
            }
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
