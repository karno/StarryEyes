using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AsyncOAuth;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.Dialogs
{
    public class KeyOverrideViewModel : ViewModel
    {
        private string _overrideConsumerKey = String.Empty;
        public string OverrideConsumerKey
        {
            get { return _overrideConsumerKey; }
            set
            {
                if (value != _overrideConsumerKey)
                {
                    _overrideConsumerKey = value;
                    RaisePropertyChanged(() => OverrideConsumerKey);
                }
            }
        }

        private string _overrideConsumerSecret = String.Empty;
        public string OverrideConsumerSecret
        {
            get { return _overrideConsumerSecret; }
            set
            {
                if (value != _overrideConsumerSecret)
                {
                    _overrideConsumerSecret = value;
                    RaisePropertyChanged(() => OverrideConsumerSecret);
                }
            }
        }

        private bool _isKeyChecking;
        public bool IsKeyChecking
        {
            get { return _isKeyChecking; }
            set
            {
                _isKeyChecking = value;
                RaisePropertyChanged(() => IsKeyChecking);
                RaisePropertyChanged(() => IsCkCsEditEnabled);
            }
        }

        public bool IsCkCsEditEnabled
        {
            get { return !_isKeyChecking; }
        }

        public void CheckAuthorize()
        {
            if (IsKeyChecking) return;
            IsKeyChecking = true;
            var authorizer = new OAuthAuthorizer(OverrideConsumerKey, OverrideConsumerSecret);
            Observable.Defer(() => authorizer.GetRequestToken(AuthorizationViewModel.RequestTokenEndpoint).ToObservable())
                .Retry(3, TimeSpan.FromSeconds(3))
                .Finally(() => IsKeyChecking = false)
                .Subscribe(_ =>
                {
                    Setting.GlobalConsumerKey.Value = this.OverrideConsumerKey;
                    Setting.GlobalConsumerSecret.Value = this.OverrideConsumerSecret;
                    UpdateEndpointKey();
                    this.Messenger.Raise(new WindowActionMessage(WindowAction.Close));
                },
                ex => this.Messenger.Raise(new TaskDialogMessage(
                                               new TaskDialogOptions
                                               {
                                                   Title = "認証失敗",
                                                   MainIcon = VistaTaskDialogIcon.Error,
                                                   MainInstruction = "API Keyの正当性を確認できませんでした。",
                                                   Content = "キーの入力を確認し、再度お試しください。",
                                                   CommonButtons = TaskDialogCommonButtons.Close,
                                                   FooterIcon = VistaTaskDialogIcon.Information,
                                                   FooterText = "Twitterの調子が悪いときやコンピュータの時計が大幅にずれている場合も認証が行えないことがあります。"
                                               })));
        }

        public void SkipAuthorize()
        {
            if (String.IsNullOrEmpty(Setting.GlobalConsumerKey.Value) && String.IsNullOrEmpty(Setting.GlobalConsumerSecret.Value))
            {
                var m = this.Messenger.GetResponse(new TaskDialogMessage(
                     new TaskDialogOptions
                     {
                         Title = "APIキー設定のスキップ",
                         MainIcon = VistaTaskDialogIcon.Warning,
                         MainInstruction = "APIキーの設定をスキップしますか？",
                         Content = "スキップする場合、最大登録可能アカウント数が2つに制限されます。",
                         CustomButtons = new[] { "スキップ", "キャンセル" },
                         ExpandedInfo = "APIキーの状況によってはアカウントが登録できないことがあります。" + Environment.NewLine +
                         "後からAPIキーを設定することもできますが、その際にすべてのアカウントを認証しなおす必要があります。"
                     }));
                if (m.Response.CustomButtonResult.HasValue && m.Response.CustomButtonResult.Value == 0)
                {
                    this.Messenger.Raise(new WindowActionMessage(WindowAction.Close));
                }
            }
            else
            {
                this.Messenger.Raise(new WindowActionMessage(WindowAction.Close));
            }
        }

        private void UpdateEndpointKey()
        {
            Setting.Accounts.Collection
                   .Select(a => a.Id)
                   .ToArray()
                   .ForEach(Setting.Accounts.RemoveAccountFromId);
        }

        #region OpenApiKeyHelpCommand
        private Livet.Commands.ViewModelCommand _openApiKeyHelpCommand;

        public Livet.Commands.ViewModelCommand OpenApiKeyHelpCommand
        {
            get
            {
                return _openApiKeyHelpCommand ??
                       (_openApiKeyHelpCommand = new Livet.Commands.ViewModelCommand(OpenApiKeyHelp));
            }
        }

        public void OpenApiKeyHelp()
        {
            BrowserHelper.Open("http://krile.starwing.net/apikey.html");
        }
        #endregion

    }
}
