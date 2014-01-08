using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using Livet;
using Livet.Messaging;
using Livet.Messaging.IO;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;
using StarryEyes.Settings.Themes;
using StarryEyes.ViewModels.Common;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.Views.Dialogs;
using StarryEyes.Views.Messaging;
using Application = System.Windows.Application;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class SettingFlipViewModel : ViewModel
    {
        private readonly MainWindowViewModel _parent;
        private ISubject<Unit> _completeCallback;

        private bool _isConfigurationActive;
        public bool IsConfigurationActive
        {
            get { return _isConfigurationActive; }
            set
            {
                if (_isConfigurationActive == value) return;
                _isConfigurationActive = value;
                MainWindowModel.SuppressKeyAssigns = value;
                MainWindowModel.SetShowMainWindowCommands(!value);
                RaisePropertyChanged();
                if (!value)
                {
                    Close();
                }
            }
        }

        public SettingFlipViewModel(MainWindowViewModel parent)
        {
            _parent = parent;
            this.CompositeDisposable.Add(Observable.FromEvent<ISubject<Unit>>(
                h => MainWindowModel.SettingRequested += h,
                h => MainWindowModel.SettingRequested -= h)
                                                   .Subscribe(this.StartSetting));
            this.CompositeDisposable.Add(
                this._accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    Setting.Accounts.Collection,
                    a => new TwitterAccountConfigurationViewModel(this, a),
                    DispatcherHelper.UIDispatcher));
        }

        private void StartSetting(ISubject<Unit> subject)
        {
            // ensure close before starting setting
            this.IsConfigurationActive = false;

            this.RefreshKeyAssignCandidates();
            this.ResetFilter();
            this._completeCallback = subject;
            this.RaisePropertyChanged();
            this.IsConfigurationActive = true;
        }

        public bool IsPowerUser
        {
            get { return Setting.IsPowerUser.Value; }
            set
            {
                Setting.IsPowerUser.Value = value;
                this.RaisePropertyChanged();
            }
        }

        #region Account control

        private DropAcceptDescription _description;
        public DropAcceptDescription DropDescription
        {
            get
            {
                if (_description == null)
                {
                    _description = new DropAcceptDescription();
                    _description.DragOver += e =>
                    {
                        var data = e.Data.GetData(typeof(TwitterAccountConfigurationViewModel)) as
                            TwitterAccountConfigurationViewModel;
                        e.Effects = data != null ? DragDropEffects.Move : DragDropEffects.None;
                    };
                    _description.DragDrop += e =>
                    {
                        var data = e.Data.GetData(typeof(TwitterAccountConfigurationViewModel)) as
                            TwitterAccountConfigurationViewModel;
                        var source = e.OriginalSource as FrameworkElement;
                        if (data == null || source == null) return;
                        var tacvm = source.DataContext as TwitterAccountConfigurationViewModel;
                        if (tacvm == null) return;
                        var origIndex = Setting.Accounts.Collection.IndexOf(data.Account);
                        var newIndex = Setting.Accounts.Collection.IndexOf(tacvm.Account);
                        if (origIndex != newIndex)
                        {
                            Setting.Accounts.Collection.Move(origIndex, newIndex);
                        }
                    };
                }
                return _description;
            }
        }

        private readonly ReadOnlyDispatcherCollectionRx<TwitterAccountConfigurationViewModel> _accounts;
        public ReadOnlyDispatcherCollectionRx<TwitterAccountConfigurationViewModel> Accounts
        {
            get { return this._accounts; }
        }

        public void AddNewAccount()
        {
            if (Setting.Accounts.Collection.Count >= 2 &&
                (Setting.GlobalConsumerKey.Value == null || Setting.GlobalConsumerSecret.Value == null) ||
                Setting.GlobalConsumerKey.Value == App.ConsumerKey)
            {
                _parent.Messenger.RaiseAsync(
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = "認証の上限",
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "認証数の上限に達しました。",
                        Content = "さらにアカウントを認証するには、Twitter API キーを登録しなければなりません。",
                        FooterIcon = VistaTaskDialogIcon.Information,
                        FooterText = "APIキーを登録すると、すべてのアカウントの登録が一旦解除されます。",
                        CommonButtons = TaskDialogCommonButtons.Close,
                    }));
                return;
            }
            var auth = new AuthorizationViewModel();
            auth.AuthorizeObservable.Subscribe(Setting.Accounts.Collection.Add);
            this._parent.Messenger.RaiseAsync(
                new TransitionMessage(typeof(AuthorizationWindow), auth, TransitionMode.Modal, null));
        }

        public async void SetApiKeys()
        {
            var reconf = "再設定";
            if (Setting.GlobalConsumerKey.Value == null)
            {
                reconf = "設定";
            }
            var resp = await this.Messenger.GetResponseAsync(
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "APIキーの" + reconf,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = "登録されたアカウントがすべて登録解除されます。",
                    Content = "APIキーの" + reconf + "を行うと、登録されたアカウントはすべてKrileから消去されます。" + Environment.NewLine + "続行しますか？",
                    CommonButtons = TaskDialogCommonButtons.YesNo
                }));
            if (resp.Response.Result == TaskDialogSimpleResult.No)
            {
                return;
            }
            var kovm = new KeyOverrideViewModel();
            this._parent.Messenger.Raise(
                new TransitionMessage(typeof(KeyOverrideWindow), kovm, TransitionMode.Modal, null));
        }

        #endregion

        #region Timeline property

        public bool IsAllowFavoriteMyself
        {
            get { return Setting.AllowFavoriteMyself.Value; }
            set { Setting.AllowFavoriteMyself.Value = value; }
        }

        public int ScrollLockStrategy
        {
            get { return (int)Setting.ScrollLockStrategy.Value; }
            set { Setting.ScrollLockStrategy.Value = (ScrollLockStrategy)value; }
        }

        public bool ShowThumbnail
        {
            get { return Setting.ShowThumbnails.Value; }
            set { Setting.ShowThumbnails.Value = value; }
        }

        public bool OpenTwitterImageWithOriginalSize
        {
            get { return Setting.OpenTwitterImageWithOriginalSize.Value; }
            set { Setting.OpenTwitterImageWithOriginalSize.Value = value; }
        }

        #endregion

        #region Mute filter editor property

        private bool _isDirtyState;

        private FilterExpressionRoot _lastCommit;

        private string _currentQueryString;
        public string QueryString
        {
            get { return _currentQueryString; }
            set
            {
                if (_currentQueryString == value) return;
                _isDirtyState = true;
                _currentQueryString = value;
                RaisePropertyChanged();
                Observable.Timer(TimeSpan.FromMilliseconds(100))
                          .Where(_ => _currentQueryString == value)
                          .Subscribe(_ => this.CheckCompileFilters(value));
            }
        }

        private bool _foundError;
        public bool FoundError
        {
            get { return _foundError; }
            set
            {
                _foundError = value;
                RaisePropertyChanged();
            }
        }

        private string _exceptionMessage;
        public string ExceptionMessage
        {
            get { return _exceptionMessage; }
            set
            {
                _exceptionMessage = value;
                RaisePropertyChanged();
            }
        }

        private async void CheckCompileFilters(string source)
        {
            try
            {
                var newFilter = await Task.Run(() => QueryCompiler.CompileFilters(source));
                newFilter.GetEvaluator(); // validate types
                newFilter.GetSqlQuery(); // validate types (phase 2)
                _lastCommit = newFilter;
                FoundError = false;
            }
            catch (Exception ex)
            {
                FoundError = true;
                ExceptionMessage = ex.Message;
            }
            _isDirtyState = false;
        }

        public void ResetFilter()
        {
            _currentQueryString = Setting.Muteds.Value.ToQuery();
            _lastCommit = null;
            FoundError = false;
            this.RaisePropertyChanged(() => QueryString);
        }

        #region OpenQueryReferenceCommand
        private Livet.Commands.ViewModelCommand _openQueryReferenceCommand;

        public Livet.Commands.ViewModelCommand OpenQueryReferenceCommand
        {
            get
            {
                if (this._openQueryReferenceCommand == null)
                {
                    this._openQueryReferenceCommand = new Livet.Commands.ViewModelCommand(OpenQueryReference);
                }
                return this._openQueryReferenceCommand;
            }
        }

        public void OpenQueryReference()
        {
            BrowserHelper.Open(App.QueryReferenceUrl);
        }
        #endregion
        #endregion

        #region Input property

        public bool IsUrlAutoEscapeEnabled
        {
            get { return Setting.IsUrlAutoEscapeEnabled.Value; }
            set { Setting.IsUrlAutoEscapeEnabled.Value = value; }
        }

        public int TweetBoxClosingAction
        {
            get { return (int)Setting.TweetBoxClosingAction.Value; }
            set { Setting.TweetBoxClosingAction.Value = (TweetBoxClosingAction)value; }
        }

        public bool IsBacktrackFallback
        {
            get { return Setting.IsBacktrackFallback.Value; }
            set { Setting.IsBacktrackFallback.Value = value; }
        }

        public bool RestorePreviousStashed
        {
            get { return Setting.RestorePreviousStashed.Value; }
            set { Setting.RestorePreviousStashed.Value = value; }
        }

        #endregion

        #region Notification and confirmation property

        public bool ConfirmOnExitApp
        {
            get { return Setting.ConfirmOnExitApp.Value; }
            set { Setting.ConfirmOnExitApp.Value = value; }
        }

        public bool WarnAmendTweet
        {
            get { return Setting.WarnAmending.Value; }
            set { Setting.WarnAmending.Value = value; }
        }

        public bool WarnReplyFromThirdAccount
        {
            get { return Setting.WarnReplyFromThirdAccount.Value; }
            set { Setting.WarnReplyFromThirdAccount.Value = value; }
        }

        #endregion

        #region Theme property

        public string BackgroundImagePath
        {
            get { return Setting.BackgroundImagePath.Value ?? String.Empty; }
            set
            {
                Setting.BackgroundImagePath.Value = value;
                RaisePropertyChanged();
            }
        }

        public void SelectBackgroundImage()
        {
            var msg = new OpeningFileSelectionMessage
            {
                Filter = "画像ファイル|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                Title = "背景画像を選択"
            };
            var resp = this.Messenger.GetResponse(msg);
            if (resp.Response != null && resp.Response.Length > 0)
            {
                BackgroundImagePath = resp.Response[0];
            }
        }

        private readonly ObservableCollection<string> _themeCandidateFiles = new ObservableCollection<string>();
        public ObservableCollection<string> ThemeCandidateFiles
        {
            get { return _keyAssignCandidateFiles; }
        }

        private string _themeCache = null;

        public int ThemeFile
        {
            get
            {
                if (_themeCache == null)
                {
                    _themeCache = Setting.Theme.Value ?? DefaultThemeProvider.DefaultThemeName;
                }
                return this._themeCandidateFiles.IndexOf(_themeCache);
            }
            set
            {
                if (value < 0) return;
                var name = DefaultThemeProvider.DefaultThemeName;
                if (value < this._themeCandidateFiles.Count)
                {
                    name = this._themeCandidateFiles[value];
                }
                _themeCache = name;
                this.RaisePropertyChanged();
            }
        }

        public void RefreshThemeCandidates()
        {
            _themeCandidateFiles.Clear();
            ThemeManager.Themes.ForEach(f => _themeCandidateFiles.Add(f));
            this.RaisePropertyChanged(() => ThemeFile);
        }

        public void ShowThemeEditor()
        {
            // todo: impl this.
        }

        private void ApplyTheme()
        {
            if (_themeCache != null)
            {
                Setting.Theme.Value = _themeCache;
            }
        }

        #endregion

        #region Key assign property

        private readonly ObservableCollection<string> _keyAssignCandidateFiles = new ObservableCollection<string>();
        public ObservableCollection<string> KeyAssignCandidateFiles
        {
            get { return this._keyAssignCandidateFiles; }
        }

        public int KeyAssignFile
        {
            get
            {
                var fn = Setting.KeyAssign.Value ?? DefaultAssignProvider.DefaultAssignName;
                return this._keyAssignCandidateFiles.IndexOf(fn);
            }
            set
            {
                if (value < 0) return; // ignore setting
                var name = DefaultAssignProvider.DefaultAssignName;
                if (value < this._keyAssignCandidateFiles.Count)
                {
                    name = this._keyAssignCandidateFiles[value];
                }
                Setting.KeyAssign.Value = name;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => KeyAssignFileContents);
            }
        }

        public void RefreshKeyAssignCandidates()
        {
            _keyAssignCandidateFiles.Clear();
            KeyAssignManager.LoadedProfiles.ForEach(f => _keyAssignCandidateFiles.Add(f));
            this.RaisePropertyChanged(() => KeyAssignFile);
            this.RaisePropertyChanged(() => KeyAssignFileContents);
        }

        public string KeyAssignFileContents
        {
            get { return KeyAssignManager.CurrentProfile.GetSourceText(); }
        }
        #endregion

        #region Outer and third party property

        public string ExternalBrowserPath
        {
            get { return Setting.ExternalBrowserPath.Value; }
            set { Setting.ExternalBrowserPath.Value = value; }
        }

        #endregion

        #region proxy configuration

        #region Web proxy

        public int UseWebProxy
        {
            get { return (int)Setting.WebProxy.Value; }
            set
            {
                Setting.WebProxy.Value = (WebProxyConfiguration)value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => ExplicitSetProxy);
            }
        }

        public bool ExplicitSetProxy
        {
            get { return Setting.WebProxy.Value == WebProxyConfiguration.Custom; }
        }

        public string WebProxyHost
        {
            get { return Setting.WebProxyHost.Value; }
            set { Setting.WebProxyHost.Value = value; }
        }

        public int WebProxyPort
        {
            get { return Setting.WebProxyPort.Value; }
            set { Setting.WebProxyPort.Value = value; }
        }

        public bool BypassProxyInLocal
        {
            get { return Setting.BypassWebProxyInLocal.Value; }
            set { Setting.BypassWebProxyInLocal.Value = value; }
        }

        public string BypassList
        {
            get { return Setting.WebProxyBypassList.Value.Guard().JoinString(Environment.NewLine); }
            set
            {
                Setting.WebProxyBypassList.Value =
                    (value ?? String.Empty).Split(new[] { Environment.NewLine },
                                                  StringSplitOptions.RemoveEmptyEntries);
            }
        }

        #endregion

        public string ApiProxy
        {
            get { return Setting.ApiProxy.Value; }
            set { Setting.ApiProxy.Value = value; }
        }

        #endregion

        #region High-level configuration

        public bool ApplyUnstablePatch
        {
            get { return Setting.AcceptUnstableVersion.Value; }
            set { Setting.AcceptUnstableVersion.Value = value; }
        }

        public bool LoadUnsafePlugin
        {
            get { return Setting.LoadUnsafePlugins.Value; }
            set { Setting.LoadUnsafePlugins.Value = value; }
        }

        public bool LoadPluginFromDevFolder
        {
            get { return Setting.LoadPluginFromDevFolder.Value; }
            set { Setting.LoadPluginFromDevFolder.Value = value; }
        }

        public bool RotateWindowContent
        {
            get { return Setting.RotateWindowContent.Value; }
            set { Setting.RotateWindowContent.Value = value; }
        }

        public int EventDisplayMinimumMillisec
        {
            get { return Setting.EventDisplayMinimumMSec.Value; }
            set { Setting.EventDisplayMinimumMSec.Value = value; }
        }

        public int EventDisplayMaximumMillisec
        {
            get { return Setting.EventDisplayMaximumMSec.Value; }
            set { Setting.EventDisplayMaximumMSec.Value = value; }
        }

        public int UserInfoReceivePeriod
        {
            get { return Setting.UserInfoReceivePeriod.Value; }
            set { Setting.UserInfoReceivePeriod.Value = value; }
        }

        public int UserRelationReceivePeriod
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
            set { Setting.UserRelationReceivePeriod.Value = value; }
        }

        public int RESTReceivePeriod
        {
            get { return Setting.RESTReceivePeriod.Value; }
            set { Setting.RESTReceivePeriod.Value = value; }
        }

        public int RESTSearchReceivePeriod
        {
            get { return Setting.RESTSearchReceivePeriod.Value; }
            set { Setting.RESTSearchReceivePeriod.Value = value; }
        }

        public int ListReceivePeriod
        {
            get { return Setting.ListReceivePeriod.Value; }
            set { Setting.ListReceivePeriod.Value = value; }
        }

        public int PostWindowTimeSec
        {
            get { return Setting.PostWindowTimeSec.Value; }
            set { Setting.PostWindowTimeSec.Value = value; }
        }

        public int PostLimitPerWindow
        {
            get { return Setting.PostLimitPerWindow.Value; }
            set { Setting.PostLimitPerWindow.Value = value; }
        }

        public bool DisableGeoLocationService
        {
            get { return Setting.DisableGeoLocationService.Value; }
            set { Setting.DisableGeoLocationService.Value = value; }
        }

        public void RestartAsMaintenance()
        {
            var psi = new ProcessStartInfo
            {
                FileName = App.ExeFilePath,
                Arguments = "-maintenance",
                UseShellExecute = true
            };
            try
            {
                MainWindowModel.SuppressCloseConfirmation = true;
                Process.Start(psi);
                Application.Current.Shutdown();
            }
            catch { }
        }

        #endregion

        public void Close()
        {
            if (!IsConfigurationActive) return;
            this.IsConfigurationActive = false;

            // refresh mute filter
            if (_isDirtyState)
            {
                try
                {
                    var newFilter = QueryCompiler.CompileFilters(_currentQueryString);
                    newFilter.GetEvaluator(); // validate types
                    _lastCommit = newFilter;
                }
                catch { }
            }
            if (_lastCommit != null)
            {
                Setting.Muteds.Value = _lastCommit;
            }

            // update connection property
            _accounts.ForEach(a => a.CommitChanges());

            // update theme
            ApplyTheme();

            // callback completion handler
            if (_completeCallback != null)
            {
                _completeCallback.OnNext(Unit.Default);
                _completeCallback.OnCompleted();
                _completeCallback = null;
            }
        }
    }

    public class TwitterAccountConfigurationViewModel : ViewModel
    {
        private bool _isConnectionPropertyHasChanged;
        private readonly SettingFlipViewModel _parent;
        private readonly TwitterAccount _account;

        public TwitterAccountConfigurationViewModel(SettingFlipViewModel parent, TwitterAccount account)
        {
            _parent = parent;
            this._account = account;
            _accounts = new DispatcherCollection<TwitterAccountViewModel>(DispatcherHelper.UIDispatcher);
            Setting.Accounts.Collection.ListenCollectionChanged()
                   .Subscribe(_ => RefreshCandidates());
            this.RefreshCandidates();
        }

        private void RefreshCandidates()
        {
            _accounts.Clear();
            Setting.Accounts.Collection
                   .Where(a => a.Id != this.Account.Id)
                   .ForEach(a => _accounts.Add(new TwitterAccountViewModel(a)));
            this.RaisePropertyChanged(() => CanFallback);
            this.RaisePropertyChanged(() => FallbackAccount);
        }

        public TwitterAccount Account
        {
            get { return this._account; }
        }

        private readonly DispatcherCollection<TwitterAccountViewModel> _accounts;
        public DispatcherCollection<TwitterAccountViewModel> Accounts
        {
            get { return this._accounts; }
        }

        public bool CanFallback
        {
            get { return Accounts.Count > 0; }
        }

        public bool IsFallbackEnabled
        {
            get { return this.Account.FallbackAccountId != null; }
            set
            {
                if (value == IsFallbackEnabled) return;
                this.Account.FallbackAccountId =
                    value
                        ? (long?)this.Accounts.Select(a => a.Id).FirstOrDefault()
                        : null;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => FallbackAccount);
            }
        }

        public TwitterAccountViewModel FallbackAccount
        {
            get
            {
                return this.Account.FallbackAccountId == null
                           ? null
                           : this.Accounts.FirstOrDefault(a => a.Id == this.Account.FallbackAccountId);
            }
            set
            {
                if (value == null)
                {
                    this.Account.FallbackAccountId = null;
                }
                else
                {
                    this.Account.FallbackAccountId = value.Id;
                }
            }
        }

        public long Id
        {
            get { return this.Account.Id; }
        }

        public Uri ProfileImage
        {
            get
            {
                if (this._account.UnreliableProfileImage == null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var user = await this._account.ShowUserAsync(this._account.Id);
                            this._account.UnreliableProfileImage = user.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            this.RaisePropertyChanged(() => ProfileImage);
                        }
                        catch { }
                    });
                }
                return this.Account.UnreliableProfileImage;
            }
        }

        public string ScreenName
        {
            get { return this.Account.UnreliableScreenName; }
        }

        public long? FallbackAccountId
        {
            get { return this.Account.FallbackAccountId; }
            set { this.Account.FallbackAccountId = value; }
        }

        public bool FallbackFavorites
        {
            get { return this.Account.IsFallbackFavorite; }
            set { this.Account.IsFallbackFavorite = value; }
        }

        public bool IsUserStreamsEnabled
        {
            get { return this.Account.IsUserStreamsEnabled; }
            set
            {
                if (IsUserStreamsEnabled == value) return;
                this.Account.IsUserStreamsEnabled = value;
                this.RaisePropertyChanged();
                this._isConnectionPropertyHasChanged = true;
            }
        }

        public bool ReceiveRepliesAll
        {
            get { return this.Account.ReceiveRepliesAll; }
            set
            {
                if (ReceiveRepliesAll == value) return;
                this.Account.ReceiveRepliesAll = value;
                this.RaisePropertyChanged();
                this._isConnectionPropertyHasChanged = true;
            }
        }

        public bool ReceiveFollowingsActivity
        {
            get { return this.Account.ReceiveFollowingsActivity; }
            set
            {
                if (ReceiveFollowingsActivity == value) return;
                this.Account.ReceiveFollowingsActivity = value;
                this.RaisePropertyChanged();
                this._isConnectionPropertyHasChanged = true;
            }
        }

        public bool IsMarkMediaAsPossiblySensitive
        {
            get { return this.Account.MarkMediaAsPossiblySensitive; }
            set { this.Account.MarkMediaAsPossiblySensitive = value; }
        }

        public void Deauthorize()
        {
            var resp = _parent.Messenger.GetResponse(new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "アカウントの削除",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "@" + ScreenName + " の認証を解除してもよろしいですか？",
                Content = "このアカウントに関するタイムラインを取得できなくなり、投稿も行えなくなります。" + Environment.NewLine +
                          "完全に認証を解除するには、Twitter公式サイトのアプリ管理ページからアクセス権を剥奪する必要があります。",
                CommonButtons = TaskDialogCommonButtons.OKCancel
            }));
            if (resp.Response.Result == TaskDialogSimpleResult.Ok)
            {
                Setting.Accounts.RemoveAccountFromId(Account.Id);
            }
        }

        public void CommitChanges()
        {
            var flag = this._isConnectionPropertyHasChanged;
            // down flags
            this._isConnectionPropertyHasChanged = false;

            // if property has changed, reconnect streams
            if (flag)
            {
                Task.Run(() => ReceiveManager.ReconnectUserStreams(_account.Id));
            }
        }
    }
}