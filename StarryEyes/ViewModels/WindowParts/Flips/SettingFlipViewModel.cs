using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Util;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging;
using Livet.Messaging.IO;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Subsystems.Notifications.UI;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Nightmare.Windows.Forms;
using StarryEyes.Properties;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;
using StarryEyes.Settings.Themes;
using StarryEyes.ViewModels.Common;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.ViewModels.WindowParts.Flips.SettingFlips;
using StarryEyes.Views.Dialogs;
using StarryEyes.Views.Messaging;
using StarryEyes.Views.Utils;
using StarryEyes.Views.WindowParts.Primitives;
using Application = System.Windows.Application;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class SettingFlipViewModel : ViewModel
    {
        private readonly MainWindowViewModel _parent;
        private ISubject<Unit> _completeCallback;
        private FileSystemWatcher _fsWatcher;

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

        // design-time support
        public SettingFlipViewModel()
        {
            if (!DesignTimeUtil.IsInDesignMode)
            {
                throw new NotSupportedException("This method should be used in design time only.");
            }
        }

        public SettingFlipViewModel(MainWindowViewModel parent)
        {
            _parent = parent;
            CompositeDisposable.Add(Observable.FromEvent<ISubject<Unit>>(
                                                  h => MainWindowModel.SettingRequested += h,
                                                  h => MainWindowModel.SettingRequested -= h)
                                              .Subscribe(StartSetting));
            CompositeDisposable.Add(
                _accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    Setting.Accounts.Collection,
                    a => new TwitterAccountConfigurationViewModel(this, a),
                    DispatcherHelper.UIDispatcher));

            // setting paramter propagation

            CompositeDisposable.Add(Setting.AutoCleanupTweets.ListenValueChanged(
                _ => RaisePropertyChanged(() => AutoCleanupStatuses)));
            CompositeDisposable.Add(Setting.AutoCleanupThreshold.ListenValueChanged(
                _ => RaisePropertyChanged(() => AutoCleanupThreshold)));
        }

        private void StartSetting(ISubject<Unit> subject)
        {
            // ensure close before starting setting
            IsConfigurationActive = false;

            RefreshKeyAssignCandidates();
            RefreshThemeCandidates();
            ResetFilter();
            KeyAssignEditorViewModel.RefreshRegisteredActions();
            _completeCallback = subject;
            _fsWatcher = new FileSystemWatcher(ThemeManager.ThemeProfileDirectoryPath, "*.xml")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName |
                               NotifyFilters.DirectoryName | NotifyFilters.Size
            };
            var refreshHandler = new FileSystemEventHandler(
                (o, e) => DispatcherHelper.UIDispatcher.InvokeAsync(RefreshThemeCandidates));
            _fsWatcher.Changed += refreshHandler;
            _fsWatcher.Created += refreshHandler;
            _fsWatcher.Deleted += refreshHandler;
            _fsWatcher.Renamed += (o, e) => DispatcherHelper.UIDispatcher.InvokeAsync(RefreshThemeCandidates);
            _fsWatcher.EnableRaisingEvents = true;
            RaisePropertyChanged();
            IsConfigurationActive = true;
        }

        public bool IsPowerUser
        {
            get { return Setting.IsPowerUser.Value; }
            set
            {
                Setting.IsPowerUser.Value = value;
                RaisePropertyChanged();
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

        public ReadOnlyDispatcherCollectionRx<TwitterAccountConfigurationViewModel> Accounts => _accounts;

        [UsedImplicitly]
        public void AddNewAccount()
        {
            if (Setting.Accounts.Collection.Count >= 2 &&
                (Setting.GlobalConsumerKey.Value == null || Setting.GlobalConsumerSecret.Value == null) ||
                Setting.GlobalConsumerKey.Value == App.ConsumerKey)
            {
                _parent.Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = SettingFlipResources.AccountLimitTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = SettingFlipResources.AccountLimitInst,
                        Content = SettingFlipResources.AccountLimitContent,
                        FooterIcon = VistaTaskDialogIcon.Information,
                        FooterText = SettingFlipResources.AccountLimitFooter,
                        CommonButtons = TaskDialogCommonButtons.Close
                    }));
                return;
            }
            var auth = new AuthorizationViewModel();
            auth.AuthorizeObservable.Subscribe(Setting.Accounts.Collection.Add);
            _parent.Messenger.RaiseSafe(() =>
                new TransitionMessage(typeof(AuthorizationWindow), auth, TransitionMode.Modal, null));
        }

        [UsedImplicitly]
        public void ResetApiKeys()
        {
            var reconf = Setting.GlobalConsumerKey.Value != null;
            var buttons = reconf
                ? new[]
                {
                    SettingFlipResources.AccountButtonResetKey,
                    SettingFlipResources.AccountButtonUseDefaultKey,
                    Resources.MsgButtonCancel
                }
                : new[] { SettingFlipResources.AccountButtonSetKey, Resources.MsgButtonCancel };
            var resp = Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = reconf
                        ? SettingFlipResources.AccountResetKeyTitle
                        : SettingFlipResources.AccountSetKeyTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = SettingFlipResources.AccountSetResetKeyInst,
                    AllowDialogCancellation = true,
                    Content = reconf
                        ? SettingFlipResources.AccountResetKeyContent
                        : SettingFlipResources.AccountSetKeyContent,
                    CommandButtons = buttons
                }));
            switch (resp.Response.CommandButtonResult ?? -1)
            {
                case 0:
                    // reconf
                    var kovm = new KeyOverrideViewModel();
                    _parent.Messenger.RaiseSafe(() =>
                        new TransitionMessage(typeof(KeyOverrideWindow), kovm, TransitionMode.Modal, null));
                    break;
                case 1:
                    if (!reconf)
                    {
                        goto default;
                    }
                    // clear authorization
                    Setting.Accounts.Collection
                           .Select(a => a.Id)
                           .ToArray()
                           .ForEach(Setting.Accounts.RemoveAccountFromId);
                    // use default key
                    Setting.GlobalConsumerKey.Value = null;
                    Setting.GlobalConsumerSecret.Value = null;
                    break;
                default:
                    //cancel
                    return;
            }
        }

        #endregion Account control

        #region Timeline property

        private int _tweetDisplayMode = (int)Setting.TweetDisplayMode.Value;

        public int TweetDisplayMode
        {
            get { return _tweetDisplayMode; }
            set
            {
                _tweetDisplayMode = value;
                Task.Run(() => ChangeDisplayOfTimeline((TweetDisplayMode)value));
                RaisePropertyChanged();
            }
        }

        private async void ChangeDisplayOfTimeline(TweetDisplayMode newValue)
        {
            if (Setting.TweetDisplayMode.Value == newValue)
            {
                return;
            }
            await DispatcherHelper.UIDispatcher.InvokeAsync(() =>
            {
                var ww = new WorkingWindow(
                    "changing timeline mode...", async () =>
                    {
                        await Task.Run(() => Setting.TweetDisplayMode.Value = newValue);
                        await DispatcherHelper.UIDispatcher.InvokeAsync(async () =>
                        {
                            await Dispatcher.Yield(DispatcherPriority.Background);
                        });
                    });
                ww.ShowDialog();
            });
        }

        public int ScrollLockStrategy
        {
            get { return (int)Setting.ScrollLockStrategy.Value; }
            set { Setting.ScrollLockStrategy.Value = (ScrollLockStrategy)value; }
        }

        public int ThumbnailStrategy
        {
            get { return (int)Setting.ThumbnailMode.Value; }
            set
            {
                Setting.ThumbnailMode.Value = (ThumbnailMode)value;
                RaisePropertyChanged();
            }
        }

        public int TimelineIconResolution
        {
            get { return (int)Setting.IconResolution.Value; }
            set { Setting.IconResolution.Value = (TimelineIconResolution)value; }
        }

        public bool IsScrollByPixel
        {
            get { return Setting.IsScrollByPixel.Value; }
            set
            {
                Setting.IsScrollByPixel.Value = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAnimateNewTweet
        {
            get { return Setting.IsAnimateScrollToNewTweet.Value; }
            set { Setting.IsAnimateScrollToNewTweet.Value = value; }
        }

        public bool IsAllowFavoriteMyself
        {
            get { return Setting.AllowFavoriteMyself.Value; }
            set { Setting.AllowFavoriteMyself.Value = value; }
        }

        public bool OpenTwitterImageWithOriginalSize
        {
            get { return Setting.OpenTwitterImageWithOriginalSize.Value; }
            set { Setting.OpenTwitterImageWithOriginalSize.Value = value; }
        }

        [UsedImplicitly]
        public void OpenDonationPage()
        {
            BrowserHelper.Open(App.DonationUrl);
        }

        public string SearchLanguage
        {
            get { return Setting.SearchLanguage.Value; }
            set
            {
                Setting.SearchLanguage.Value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => SearchLanguageDescription);
            }
        }

        public IEnumerable<string> SupportedLanguages
        {
            get
            {
                return new[]
                {
                    "", "aa", "ab", "ae", "af", "ak", "am", "an", "ar", "as", "av", "ay", "az", "ba", "be", "bg", "bh",
                    "bi", "bm", "bn", "bo", "br", "bs", "ca", "ce", "ch", "co", "cr", "cs", "cu", "cv", "cy", "da",
                    "de",
                    "dv", "dz", "ee", "el", "en", "eo", "es", "et", "eu", "fa", "ff", "fi", "fj", "fo", "fr", "fy",
                    "ga",
                    "gd", "gl", "gn", "gu", "gv", "ha", "he", "hi", "ho", "hr", "ht", "hu", "hy", "hz", "ia", "id",
                    "ie",
                    "ig", "ii", "ik", "io", "is", "it", "iu", "ja", "jv", "ka", "kg", "ki", "kj", "kk", "kl", "km",
                    "kn",
                    "ko", "kr", "ks", "ku", "kv", "kw", "ky", "la", "lb", "lg", "li", "ln", "lo", "lt", "lu", "lv",
                    "mg",
                    "mh", "mi", "mk", "ml", "mn", "mr", "ms", "mt", "my", "na", "nb", "nd", "ne", "ng", "nl", "nn",
                    "no",
                    "nr", "nv", "ny", "oc", "oj", "om", "or", "os", "pa", "pi", "pl", "ps", "pt", "qu", "rm", "rn",
                    "ro",
                    "ru", "rw", "sa", "sc", "sd", "se", "sg", "si", "sk", "sl", "sm", "sn", "so", "sq", "sr", "ss",
                    "st",
                    "su", "sv", "sw", "ta", "te", "tg", "th", "ti", "tk", "tl", "tn", "to", "tr", "ts", "tt", "tw",
                    "ty",
                    "ug", "uk", "ur", "uz", "ve", "vi", "vo", "wa", "wo", "xh", "yi", "yo", "za", "zh", "zu"
                };
            }
        }

        public string SearchLanguageDescription
        {
            get
            {
                try
                {
                    var culture = new CultureInfo(SearchLanguage);
                    if (CultureInfo.InvariantCulture.Equals(culture))
                    {
                        return SettingFlipResources.TimelineSearchLanguageUnspecified;
                    }
                    return culture.DisplayName + " (" + culture.EnglishName + ")";
                }
                catch (CultureNotFoundException)
                {
                    return SettingFlipResources.TimelineSearchLanguageInvalid;
                }
            }
        }

        public bool AutoCleanupStatuses
        {
            get { return Setting.AutoCleanupTweets.Value; }
            set
            {
                Setting.AutoCleanupTweets.Value = value;
                RaisePropertyChanged();
            }
        }

        public int AutoCleanupThreshold
        {
            get { return Math.Max(Setting.AutoCleanupThreshold.Value, 0); }
            set
            {
                Setting.AutoCleanupThreshold.Value = Math.Max(value, 0);
                RaisePropertyChanged();
            }
        }

        #endregion Timeline property

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
                          .Subscribe(_ => CheckCompileFilters(value));
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
            RaisePropertyChanged(() => QueryString);
        }

        #region OpenQueryReferenceCommand

        private Livet.Commands.ViewModelCommand _openQueryReferenceCommand;

        public Livet.Commands.ViewModelCommand OpenQueryReferenceCommand
        {
            get
            {
                if (_openQueryReferenceCommand == null)
                {
                    _openQueryReferenceCommand = new Livet.Commands.ViewModelCommand(OpenQueryReference);
                }
                return _openQueryReferenceCommand;
            }
        }

        public void OpenQueryReference()
        {
            BrowserHelper.Open(App.QueryReferenceUrl);
        }

        #endregion OpenQueryReferenceCommand

        public bool UseLightweightMute
        {
            get { return Setting.UseLightweightMute.Value; }
            set { Setting.UseLightweightMute.Value = value; }
        }

        public bool IsMuteBlockingUsers
        {
            get { return Setting.MuteBlockingUsers.Value; }
            set { Setting.MuteBlockingUsers.Value = value; }
        }

        public bool IsMuteNoRetweetUsersRetweet
        {
            get { return Setting.MuteNoRetweets.Value; }
            set { Setting.MuteNoRetweets.Value = value; }
        }

        public bool IsMuteOfficialMutes
        {
            get { return Setting.MuteOfficialMutes.Value; }
            set { Setting.MuteOfficialMutes.Value = value; }
        }

        #endregion Mute filter editor property

        #region Input property

        public int TweetBoxClosingActionIndex
        {
            get { return (int)Setting.TweetBoxClosingAction.Value; }
            set { Setting.TweetBoxClosingAction.Value = (TweetBoxClosingAction)value; }
        }

        public bool IsBacktrackFallback
        {
            get { return Setting.IsBacktrackFallback.Value; }
            set { Setting.IsBacktrackFallback.Value = value; }
        }

        public bool IsInputSuggestEnabled
        {
            get { return Setting.IsInputSuggestEnabled.Value; }
            set
            {
                Setting.IsInputSuggestEnabled.Value = value;
                RaisePropertyChanged();
            }
        }

        public int InputUserSuggestActionIndex
        {
            get { return (int)Setting.InputUserSuggestMode.Value; }
            set
            {
                Setting.InputUserSuggestMode.Value = (InputUserSuggestMode)value;
                RaisePropertyChanged();
            }
        }

        public bool RestorePreviousStashed
        {
            get { return Setting.RestorePreviousStashed.Value; }
            set { Setting.RestorePreviousStashed.Value = value; }
        }

        public bool SuppressTagBindInReply
        {
            get { return Setting.SuppressTagBindingInReply.Value; }
            set { Setting.SuppressTagBindingInReply.Value = value; }
        }

        public bool NewTextCounting
        {
            get { return Setting.NewTextCounting.Value; }
            set
            {
                Setting.NewTextCounting.Value = value;
                _parent.InputViewModel.InputCoreViewModel.UpdateTextCount();
            }
        }

        #endregion Input property

        #region Notification and confirmation property

        public IEnumerable<string> Displays
        {
            get
            {
                return new[] { SettingFlipResources.NotifyMainDisplay }
                    .Concat(Screen.AllScreens.Select((s, i) => "[" + i + "]" + s.DeviceName));
            }
        }

        public int TargetDisplay
        {
            get { return Setting.NotifyScreenIndex.Value + 1; }
            set
            {
                var newValue = value - 1;
                if (Setting.NotifyScreenIndex.Value == newValue) return;
                Setting.NotifyScreenIndex.Value = newValue;
                DisplayMarkerViewModel.ShowMarker(newValue);
            }
        }

        public int NotificationTypeIndex
        {
            get { return (int)Setting.NotificationType.Value; }
            set
            {
                Setting.NotificationType.Value = (NotificationUIType)value;
                RaisePropertyChanged();
            }
        }

        public bool IsNotifyWhenWindowIsActive
        {
            get { return Setting.NotifyWhenWindowIsActive.Value; }
            set { Setting.NotifyWhenWindowIsActive.Value = value; }
        }

        public bool IsNotifyBackfilledTweets
        {
            get { return Setting.NotifyBackfilledTweets.Value; }
            set { Setting.NotifyBackfilledTweets.Value = value; }
        }

        public bool IsNotifyMentions
        {
            get { return Setting.NotifyMention.Value; }
            set { Setting.NotifyMention.Value = value; }
        }

        public bool IsNotifyMessages
        {
            get { return Setting.NotifyMessage.Value; }
            set { Setting.NotifyMessage.Value = value; }
        }

        public bool IsNotifyFollows
        {
            get { return Setting.NotifyFollow.Value; }
            set { Setting.NotifyFollow.Value = value; }
        }

        public bool IsNotifyFavorites
        {
            get { return Setting.NotifyFavorite.Value; }
            set { Setting.NotifyFavorite.Value = value; }
        }

        public bool IsNotifyRetweets
        {
            get { return Setting.NotifyRetweet.Value; }
            set { Setting.NotifyRetweet.Value = value; }
        }

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

        public bool ShowMessageOnTweetFailed
        {
            get { return Setting.ShowMessageOnTweetFailed.Value; }
            set { Setting.ShowMessageOnTweetFailed.Value = value; }
        }

        [UsedImplicitly]
        public void ClearAllTabNotification()
        {
            TabManager.Columns.SelectMany(c => c.Tabs).ForEach(t => t.NotifyNewArrivals = false);
            _parent.Messenger.RaiseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = SettingFlipResources.NotifyAllTabDisabledTitle,
                    MainIcon = VistaTaskDialogIcon.Information,
                    MainInstruction = SettingFlipResources.NotifyAllTabDisabledInst,
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        #endregion Notification and confirmation property

        #region Theme property

        public string BackgroundImagePath
        {
            get { return Setting.BackgroundImagePath.Value ?? String.Empty; }
            set
            {
                Setting.BackgroundImagePath.Value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => WallpaperImage);
            }
        }

        public int BackgroundImageTransparency
        {
            get { return Setting.BackgroundImageTransparency.Value; }
            set
            {
                Setting.BackgroundImageTransparency.Value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => WallpaperOpacity);
            }
        }

        [UsedImplicitly]
        public void SelectBackgroundImage()
        {
            var resp = Messenger.GetResponseSafe(() =>
            {
                var msg = new OpeningFileSelectionMessage
                {
                    Title = SettingFlipResources.ThemeOpenPictureTitle,
                    Filter = SettingFlipResources.ThemeOpenPictureFilter + "|*.png;*.jpg;*.jpeg;*.gif;*.bmp"
                };
                if (!String.IsNullOrEmpty(BackgroundImagePath))
                {
                    msg.FileName = BackgroundImagePath;
                }
                return msg;
            });
            if (resp.Response != null && resp.Response.Length > 0)
            {
                BackgroundImagePath = resp.Response[0];
            }
        }

        private readonly ObservableCollection<string> _themeCandidateFiles = new ObservableCollection<string>();

        public ObservableCollection<string> ThemeCandidateFiles => _themeCandidateFiles;

        private string _themeCache;

        public int ThemeFileIndex
        {
            get
            {
                if (_themeCache == null)
                {
                    _themeCache = Setting.Theme.Value ?? BuiltInThemeProvider.DefaultThemeName;
                }
                return _themeCandidateFiles.IndexOf(_themeCache);
            }
            set
            {
                if (value < 0) return;
                var name = BuiltInThemeProvider.DefaultThemeName;
                if (value < _themeCandidateFiles.Count)
                {
                    name = _themeCandidateFiles[value];
                }
                _themeCache = name;
                CurrentThemeChanged();
                RaisePropertyChanged();
            }
        }

        public void RefreshThemeCandidates()
        {
            _themeCandidateFiles.Clear();
            ThemeManager.ReloadCandidates();
            ThemeManager.Themes.ForEach(f => _themeCandidateFiles.Add(f));
            RaisePropertyChanged(() => ThemeFileIndex);
            CurrentThemeChanged();
        }

        public void OpenThemeFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "EXPLORER.EXE",
                    Arguments = "/n, " + ThemeManager.ThemeProfileDirectoryPath,
                });
            }
            catch
            {
            }
        }

        public void ShowThemeEditor()
        {
            // todo: impl this.
        }

        private void ApplyTheme()
        {
            if (_themeCache != null && _themeCache != Setting.Theme.Value)
            {
                Setting.Theme.Value = _themeCache;
                // refresh colors in timeline
                TimelineSwapResourcesBehavior.RefreshResources();
            }
        }

        #region for theme preview

        private ThemeProfile CurrentConfiguringTheme
        {
            get
            {
                return (ThemeFileIndex >= 0 && ThemeFileIndex < _themeCandidateFiles.Count
                           ? ThemeManager.GetTheme(_themeCandidateFiles[ThemeFileIndex])
                           : null) ?? BuiltInThemeProvider.GetDefault();
            }
        }

        private void CurrentThemeChanged()
        {
            RaisePropertyChanged(() => GlobalForeground);
            RaisePropertyChanged(() => GlobalBackground);
            RaisePropertyChanged(() => GlobalKeyBrush);
            RaisePropertyChanged(() => CurrentThemeBorder);
            RaisePropertyChanged(() => CurrentThemeBorder);
            RaisePropertyChanged(() => TitleBackground);
            RaisePropertyChanged(() => TitleForeground);
            RaisePropertyChanged(() => ActiveTabForeground);
            RaisePropertyChanged(() => InactiveTabForeground);
            RaisePropertyChanged(() => TabUnreadCountForeground);
        }

        public Brush GlobalForeground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.BaseColor.Foreground); }
        }

        public Brush GlobalBackground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.BaseColor.Background); }
        }

        public Brush GlobalKeyBrush
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.GlobalKeyColor); }
        }

        public Brush CurrentThemeBorder
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.GlobalKeyColor); }
        }

        public Brush TitleBackground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.TitleBarColor.Background); }
        }

        public Brush TitleForeground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.TitleBarColor.Foreground); }
        }

        public Brush ActiveTabForeground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.TabColor.Focused); }
        }

        public Brush InactiveTabForeground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.TabColor.Default); }
        }

        public Brush TabUnreadCountForeground
        {
            get { return new SolidColorBrush(CurrentConfiguringTheme.TabColor.UnreadCount); }
        }

        public BitmapImage WallpaperImage
        {
            get
            {
                var uri = BackgroundImagePath;
                if (uri == null)
                {
                    return null;
                }
                try
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.UriSource = new Uri(uri);
                    bi.EndInit();
                    return bi;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public double WallpaperOpacity
        {
            get { return (255 - Math.Min(255, Setting.BackgroundImageTransparency.Value)) / 255.0; }
        }

        #endregion for theme preview

        #endregion Theme property

        #region Key assign property

        private readonly KeyAssignEditorViewModel _keyAssignEditorViewModel = new KeyAssignEditorViewModel();

        public KeyAssignEditorViewModel KeyAssignEditorViewModel
        {
            get { return _keyAssignEditorViewModel; }
        }

        private readonly ObservableCollection<string> _keyAssignCandidateFiles =
            new ObservableCollection<string>();

        public ObservableCollection<string> KeyAssignCandidateFiles
        {
            get { return _keyAssignCandidateFiles; }
        }

        public int KeyAssignFile
        {
            get
            {
                var fn = Setting.KeyAssign.Value ?? DefaultAssignProvider.DefaultAssignName;
                return _keyAssignCandidateFiles.IndexOf(fn);
            }
            set
            {
                if (value < 0) return; // ignore setting
                var name = DefaultAssignProvider.DefaultAssignName;
                if (value < _keyAssignCandidateFiles.Count)
                {
                    name = _keyAssignCandidateFiles[value];
                }
                Setting.KeyAssign.Value = name;
                _keyAssignEditorViewModel.Profile = KeyAssignManager.CurrentProfile;
                RaisePropertyChanged();
            }
        }

        public void RefreshKeyAssignCandidates()
        {
            _keyAssignCandidateFiles.Clear();
            KeyAssignManager.ReloadCandidates();
            KeyAssignManager.LoadedProfiles.ForEach(f => _keyAssignCandidateFiles.Add(f));
            RaisePropertyChanged(() => KeyAssignFile);
            _keyAssignEditorViewModel.Commit();
            _keyAssignEditorViewModel.Profile = KeyAssignManager.CurrentProfile;
        }

        [UsedImplicitly]
        public void AddNewKeyAssign()
        {
            var response = Messenger.GetResponseSafe(() => new TransitionMessage(typeof(AddNewKeyAssignWindow),
                new AddNewKeyAssignDialogViewModel(), TransitionMode.Modal));
            var tranvm = (AddNewKeyAssignDialogViewModel)response.TransitionViewModel;
            if (tranvm.Result)
            {
                var assign = new KeyAssignProfile(tranvm.FileName);
                if (tranvm.IsCreateAsCopy)
                {
                    assign.SetAssigns(
                        KeyAssignManager.CurrentProfile
                                        .ToString()
                                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                }
                assign.Save(KeyAssignManager.KeyAssignsProfileDirectoryPath);
                RefreshKeyAssignCandidates();
            }
        }

        [UsedImplicitly]
        public void DeleteCurrentKeyAssign()
        {
            var response = Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = SettingFlipResources.KeyAssignDeleteTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = SettingFlipResources.KeyAssignDeleteInst,
                    Content =
                        SettingFlipResources.KeyAssignDeleteContentFormat.SafeFormat(
                            KeyAssignManager.CurrentProfile.Name),
                    CommonButtons = TaskDialogCommonButtons.OKCancel
                }));
            if (response.Response.Result != TaskDialogSimpleResult.Ok) return;
            try
            {
                var path = KeyAssignManager.CurrentProfile.GetFilePath(
                    KeyAssignManager.KeyAssignsProfileDirectoryPath);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else
                {
                    throw new InvalidOperationException("file " + path + " does not exist.");
                }
                KeyAssignEditorViewModel.ClearCurrentProfile();
            }
            catch (Exception ex)
            {
                Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = SettingFlipResources.KeyAssignDeleteFailedTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = SettingFlipResources.KeyAssignDeleteFailedInst,
                        Content = ex.Message,
                        ExpandedInfo = ex.ToString(),
                        CommonButtons = TaskDialogCommonButtons.Close
                    }));
            }
            RefreshKeyAssignCandidates();
        }

        #endregion Key assign property

        #region Outer and third party property

        public string ExternalBrowserPath
        {
            get { return Setting.ExternalBrowserPath.Value; }
            set { Setting.ExternalBrowserPath.Value = value; }
        }

        #endregion Outer and third party property

        #region proxy configuration

        #region Web proxy

        public int UseWebProxy
        {
            get { return (int)Setting.WebProxyType.Value; }
            set
            {
                Setting.WebProxyType.Value = (WebProxyConfiguration)value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => ExplicitSetProxy);
            }
        }

        public bool ExplicitSetProxy
        {
            get { return Setting.WebProxyType.Value == WebProxyConfiguration.Custom; }
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

        #endregion Web proxy

        public string ApiProxy
        {
            get { return Setting.ApiProxy.Value; }
            set { Setting.ApiProxy.Value = value; }
        }

        #endregion proxy configuration

        #region High-level configuration

        public bool ApplyUnstablePatch
        {
            get { return Setting.AcceptUnstableVersion.Value; }
            set { Setting.AcceptUnstableVersion.Value = value; }
        }

        public bool UseInMemoryDatabase
        {
            get { return Setting.UseInMemoryDatabase.Value; }
            set { Setting.UseInMemoryDatabase.Value = value; }
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

        public string SearchLocale
        {
            get { return Setting.SearchLocale.Value; }
            set { Setting.SearchLocale.Value = value; }
        }

        public bool LoadPluginFromDevFolder
        {
            get { return Setting.LoadPluginFromDevFolder.Value; }
            set { Setting.LoadPluginFromDevFolder.Value = value; }
        }

        public bool DisableGeoLocationService
        {
            get { return Setting.DisableGeoLocationService.Value; }
            set { Setting.DisableGeoLocationService.Value = value; }
        }

        public bool IsBehaviorLogEnabled
        {
            get { return Setting.IsBehaviorLogEnabled.Value; }
            set { Setting.IsBehaviorLogEnabled.Value = value; }
        }

        [UsedImplicitly]
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
            catch
            {
            }
        }

        #endregion High-level configuration

        public void Close()
        {
            if (!IsConfigurationActive) return;
            IsConfigurationActive = false;

            // refresh mute filter
            if (_isDirtyState)
            {
                try
                {
                    var newFilter = QueryCompiler.CompileFilters(_currentQueryString);
                    newFilter.GetEvaluator(); // validate types
                    _lastCommit = newFilter;
                }
                catch
                {
                }
            }
            if (_lastCommit != null)
            {
                Setting.Muteds.Value = _lastCommit;
            }

            // update connection property
            _accounts.ForEach(a => a.CommitChanges());

            // dispose fswatcher
            _fsWatcher.Dispose();

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
            _account = account;
            _accounts = new DispatcherCollection<TwitterAccountViewModel>(DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(Setting.Accounts.Collection.ListenCollectionChanged(_ => RefreshCandidates()));
            RefreshCandidates();
        }

        private void RefreshCandidates()
        {
            _accounts.Clear();
            Setting.Accounts.Collection
                   .Where(a => a.Id != Account.Id)
                   .ForEach(a => _accounts.Add(new TwitterAccountViewModel(a)));
            RaisePropertyChanged(() => CanFallback);
            RaisePropertyChanged(() => FallbackAccount);
        }

        public TwitterAccount Account => _account;

        private readonly DispatcherCollection<TwitterAccountViewModel> _accounts;

        public DispatcherCollection<TwitterAccountViewModel> Accounts => _accounts;

        public bool CanFallback => Accounts.Count > 0;

        public bool IsFallbackEnabled
        {
            get { return Account.FallbackAccountId != null; }
            set
            {
                if (value == IsFallbackEnabled) return;
                Account.FallbackAccountId =
                    value
                        ? (long?)Accounts.Select(a => a.Id).FirstOrDefault()
                        : null;
                RaisePropertyChanged();
                RaisePropertyChanged(() => FallbackAccount);
            }
        }

        public TwitterAccountViewModel FallbackAccount
        {
            get
            {
                return Account.FallbackAccountId == null
                    ? null
                    : Accounts.FirstOrDefault(a => a.Id == Account.FallbackAccountId);
            }
            set
            {
                if (value == null)
                {
                    Account.FallbackAccountId = null;
                }
                else
                {
                    Account.FallbackAccountId = value.Id;
                }
            }
        }

        public long Id => Account.Id;

        public Uri ProfileImage
        {
            get
            {
                if (_account.UnreliableProfileImage == null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var user = await _account
                                .CreateAccessor().ShowUserAsync(new UserParameter(_account.Id),
                                    CancellationToken.None);
                            _account.UnreliableProfileImage =
                                user.Result.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            RaisePropertyChanged(() => ProfileImage);
                        }
                        catch
                        {
                        }
                    });
                }
                return Account.UnreliableProfileImage;
            }
        }

        public string ScreenName
        {
            get { return Account.UnreliableScreenName; }
        }

        public long? FallbackAccountId
        {
            get { return Account.FallbackAccountId; }
            set { Account.FallbackAccountId = value; }
        }

        public bool FallbackFavorites
        {
            get { return Account.IsFallbackFavorite; }
            set { Account.IsFallbackFavorite = value; }
        }

        public bool IsUserStreamsEnabled
        {
            get { return Account.IsUserStreamsEnabled; }
            set
            {
                if (IsUserStreamsEnabled == value) return;
                Account.IsUserStreamsEnabled = value;
                RaisePropertyChanged();
                _isConnectionPropertyHasChanged = true;
            }
        }

        public bool ReceiveRepliesAll
        {
            get { return Account.ReceiveRepliesAll; }
            set
            {
                if (ReceiveRepliesAll == value) return;
                Account.ReceiveRepliesAll = value;
                RaisePropertyChanged();
                _isConnectionPropertyHasChanged = true;
            }
        }

        public bool ReceiveFollowingsActivity
        {
            get { return Account.ReceiveFollowingsActivity; }
            set
            {
                if (ReceiveFollowingsActivity == value) return;
                Account.ReceiveFollowingsActivity = value;
                RaisePropertyChanged();
                _isConnectionPropertyHasChanged = true;
            }
        }

        public bool IsMarkMediaAsPossiblySensitive
        {
            get { return Account.MarkMediaAsPossiblySensitive; }
            set { Account.MarkMediaAsPossiblySensitive = value; }
        }

        [UsedImplicitly]
        public void Unauthorize()
        {
            var resp = _parent.Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = SettingFlipResources.AccountUnauthorizeTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction =
                        SettingFlipResources
                            .AccountUnauthorizeInstFormat.SafeFormat("@" + ScreenName),
                    Content = SettingFlipResources.AccountUnauthorizeContent,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = SettingFlipResources.AccountUnauthorizeFooter,
                    CommonButtons = TaskDialogCommonButtons.OKCancel
                }));
            if (resp.Response.Result == TaskDialogSimpleResult.Ok)
            {
                Setting.Accounts.RemoveAccountFromId(Account.Id);
            }
        }

        public void CommitChanges()
        {
            var flag = _isConnectionPropertyHasChanged;
            // down flags
            _isConnectionPropertyHasChanged = false;

            // if property has changed, reconnect streams
            if (flag)
            {
                Task.Run(() => ReceiveManager.ReconnectUserStreams(_account.Id));
            }
        }
    }
}