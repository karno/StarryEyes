using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AsyncOAuth;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Globalization.Dialogs;
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

        [UsedImplicitly]
        public void CheckAuthorize()
        {
            if (OverrideConsumerKey == App.ConsumerKey)
            {
                this.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = KeyOverrideWindowResources.MsgKeySettingError,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = KeyOverrideWindowResources.MsgBlockingKeyInst,
                    Content = KeyOverrideWindowResources.MsgBlockingKeyContent,
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
                return;
            }
            if (!(System.Text.RegularExpressions.Regex.Match(OverrideConsumerKey, "^[a-zA-Z0-9]+$")).Success)
            {
                this.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = KeyOverrideWindowResources.MsgKeySettingError,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = KeyOverrideWindowResources.MsgInvalidKeyInst,
                    Content = KeyOverrideWindowResources.MsgInvalidKeyContent,
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
                return;
            }
            if (!(System.Text.RegularExpressions.Regex.Match(OverrideConsumerSecret, "^[a-zA-Z0-9]+$")).Success)
            {
                this.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = KeyOverrideWindowResources.MsgKeySettingError,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = KeyOverrideWindowResources.MsgInvalidSecretInst,
                    Content = KeyOverrideWindowResources.MsgInvalidSecretContent,
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
                return;
            }
            if (IsKeyChecking) return;
            IsKeyChecking = true;
            var authorizer = new OAuthAuthorizer(OverrideConsumerKey, OverrideConsumerSecret);
            Observable.Defer(
                () => authorizer.GetRequestToken(AuthorizationViewModel.RequestTokenEndpoint).ToObservable())
                      .Retry(3, TimeSpan.FromSeconds(3))
                      .Finally(() => IsKeyChecking = false)
                      .Subscribe(
                          _ =>
                          {
                              Setting.GlobalConsumerKey.Value = this.OverrideConsumerKey;
                              Setting.GlobalConsumerSecret.Value = this.OverrideConsumerSecret;
                              UpdateEndpointKey();
                              this.Messenger.RaiseSafe(() => new WindowActionMessage(WindowAction.Close));
                          },
                          ex => this.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                          {
                              Title = KeyOverrideWindowResources.MsgAuthErrorTitle,
                              MainIcon = VistaTaskDialogIcon.Error,
                              MainInstruction = KeyOverrideWindowResources.MsgAuthErrorInst,
                              Content = KeyOverrideWindowResources.MsgAuthErrorContent,
                              CommonButtons = TaskDialogCommonButtons.Close,
                              FooterIcon = VistaTaskDialogIcon.Information,
                              FooterText = KeyOverrideWindowResources.MsgAuthErrorFooter,
                          })));
        }

        [UsedImplicitly]
        public void SkipAuthorize()
        {
            if (String.IsNullOrEmpty(Setting.GlobalConsumerKey.Value) &&
                String.IsNullOrEmpty(Setting.GlobalConsumerSecret.Value))
            {
                var m = this.Messenger.GetResponseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = KeyOverrideWindowResources.MsgSkipTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = KeyOverrideWindowResources.MsgSkipInst,
                    Content = KeyOverrideWindowResources.MsgSkipContent,
                    CommonButtons = TaskDialogCommonButtons.OKCancel,
                    ExpandedInfo = KeyOverrideWindowResources.MsgSkipExInfo
                }));
                if (m.Response.Result == TaskDialogSimpleResult.Ok)
                {
                    this.Messenger.RaiseSafe(() => new WindowActionMessage(WindowAction.Close));
                }
            }
            else
            {
                this.Messenger.RaiseSafe(() => new WindowActionMessage(WindowAction.Close));
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
