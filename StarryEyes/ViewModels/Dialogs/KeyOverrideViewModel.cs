using System;
using System.Linq;
using System.Reactive.Linq;
using Codeplex.OAuth;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Breezy.Api;
using StarryEyes.Models.Stores;
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

        private bool _isKeyChecking = false;
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
            Observable.Defer(() => authorizer.GetRequestToken(AuthorizationViewModel.RequestTokenEndpoint))
                .Retry(3, TimeSpan.FromSeconds(3))
                .Finally(() => IsKeyChecking = false)
                .Subscribe(_ =>
                {
                    Setting.GlobalConsumerKey.Value = this.OverrideConsumerKey;
                    Setting.GlobalConsumerSecret.Value = this.OverrideConsumerSecret;
                    UpdateEndpointKey();
                    this.Messenger.Raise(new WindowActionMessage(null, WindowAction.Close));
                },
                ex =>
                {
                    this.Messenger.Raise(new TaskDialogMessage(
                        new TaskDialogInterop.TaskDialogOptions()
                        {
                            Title = "認証失敗",
                            MainIcon = TaskDialogInterop.VistaTaskDialogIcon.Error,
                            MainInstruction = "API Keyの正当性を確認できませんでした。",
                            Content = "キーの入力を確認し、再度お試しください。",
                            CommonButtons = TaskDialogInterop.TaskDialogCommonButtons.Close,
                            FooterIcon = TaskDialogInterop.VistaTaskDialogIcon.Information,
                            FooterText = "Twitterの調子が悪いときやコンピュータの時計が大幅にずれている場合も認証が行えないことがあります。"
                        }));
                });
        }

        public void SkipAuthorize()
        {
            if (String.IsNullOrEmpty(Setting.GlobalConsumerKey.Value) && String.IsNullOrEmpty(Setting.GlobalConsumerSecret.Value))
            {
                var result = TaskDialogInterop.TaskDialog.ShowMessage(
                    System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().SingleOrDefault(x => x.IsActive),
                    "APIキー設定のスキップ",
                    "APIキーの設定をスキップしますか？",
                    "スキップする場合、いくつか制限が適用されます。" + Environment.NewLine +
                    "後からもキーを設定できますが、その際にすべてのアカウントを認証しなおす必要があります。",
                    "APIキーの状況によってはアカウントが登録できないことがあります。" + Environment.NewLine +
                    "また、最大登録可能アカウント数も制限されます。",
                    "",
                    "",
                    TaskDialogInterop.TaskDialogCommonButtons.OKCancel,
                    TaskDialogInterop.VistaTaskDialogIcon.Warning,
                    TaskDialogInterop.VistaTaskDialogIcon.None
                    );
                if (result == TaskDialogInterop.TaskDialogSimpleResult.Ok)
                {
                    this.Messenger.Raise(new WindowActionMessage(null, WindowAction.Close));
                }
            }
            else
            {
                this.Messenger.Raise(new WindowActionMessage(null, WindowAction.Close));
            }
        }

        private void UpdateEndpointKey()
        {
            ApiEndpoint.DefaultConsumerKey = Setting.GlobalConsumerKey.Value ?? App.ConsumerKey;
            ApiEndpoint.DefaultConsumerSecret = Setting.GlobalConsumerSecret.Value ?? App.ConsumerSecret;
            AccountsStore.Accounts.Clear();
        }

        #region OpenApiKeyHelpCommand
        private Livet.Commands.ViewModelCommand _OpenApiKeyHelpCommand;

        public Livet.Commands.ViewModelCommand OpenApiKeyHelpCommand
        {
            get
            {
                if (_OpenApiKeyHelpCommand == null)
                {
                    _OpenApiKeyHelpCommand = new Livet.Commands.ViewModelCommand(OpenApiKeyHelp);
                }
                return _OpenApiKeyHelpCommand;
            }
        }

        public void OpenApiKeyHelp()
        {
            BrowserHelper.Open("http://krile.starwing.net/apikey.html");
        }
        #endregion

    }
}
