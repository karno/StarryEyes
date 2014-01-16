using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Xaml;
using StarryEyes.Albireo;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Nightmare.Windows;

namespace StarryEyes.Settings
{
    public static class Setting
    {
        private static ConcurrentDictionary<string, object> _settingValueHolder;

        public static readonly SettingItemStruct<bool> IsPowerUser =
            new SettingItemStruct<bool>("IsPowerUser", false);

        #region Tab and Columns

        private static readonly SettingItem<ColumnDescription[]> _columns =
            new SettingItem<ColumnDescription[]>("Columns", null);

        internal static IEnumerable<ColumnDescription> Columns
        {
            get { return _columns.Value ?? GenerateEmptyTabs(); }
            set { _columns.Value = value.Guard().ToArray(); }
        }

        private static IEnumerable<ColumnDescription> GenerateEmptyTabs()
        {
            return new[]
            {
                new ColumnDescription
                {
                    Tabs = new[]
                    {
                        CommonTabBuilder.GenerateGeneralTab(),
                        CommonTabBuilder.GenerateHomeTab(),
                        CommonTabBuilder.GenerateMentionTab(),
                        CommonTabBuilder.GenerateMeTab()
                    }.Select(t => new TabDescription(t)).ToArray()
                },
                new ColumnDescription
                {
                    Tabs = new[] {CommonTabBuilder.GenerateActivitiesTab()}
                        .Select(t => new TabDescription(t)).ToArray()
                }
            };
        }

        internal static void ResetTabInfo()
        {
            _columns.Value = null;
        }

        #endregion

        #region Authentication and accounts

        private static readonly SettingItem<List<TwitterAccount>> _accounts =
            new SettingItem<List<TwitterAccount>>("Accounts", new List<TwitterAccount>());

        private static AccountManager _manager;
        public static AccountManager Accounts
        {
            get { return _manager; }
        }

        public static readonly SettingItem<string> GlobalConsumerKey =
            new SettingItem<string>("GlobalConsumerKey", null);

        public static readonly SettingItem<string> GlobalConsumerSecret =
            new SettingItem<string>("GlobalConsumerSecret", null);

        #endregion

        #region Timeline display and action

        public static readonly FilterSettingItem Muteds = new FilterSettingItem("Muteds");

        public static readonly SettingItemStruct<bool> AllowFavoriteMyself =
            new SettingItemStruct<bool>("AllowFavoriteMyself", false);

        public static readonly SettingItemStruct<ScrollLockStrategy> ScrollLockStrategy =
            new SettingItemStruct<ScrollLockStrategy>("ScrollLockStrategy", Settings.ScrollLockStrategy.WhenScrolled);

        public static readonly SettingItem<string> Theme =
            new SettingItem<string>("Theme", null);

        public static readonly SettingItem<string> BackgroundImagePath =
            new SettingItem<string>("BackgroundImagePath", null);

        public static readonly SettingItemStruct<int> BackgroundImageTransparency =
            new SettingItemStruct<int>("BackgroundImageTransparency", 0);

        public static readonly SettingItem<string> KeyAssign =
            new SettingItem<string>("KeyAssign", null);

        public static readonly SettingItemStruct<bool> ShowThumbnails =
            new SettingItemStruct<bool>("ShowThumbnails", true);

        public static readonly SettingItemStruct<bool> OpenTwitterImageWithOriginalSize =
            new SettingItemStruct<bool>("OpenTwitterImageWithOriginalSize", true);

        #endregion

        #region Input control

        public static readonly SettingItemStruct<bool> IsUrlAutoEscapeEnabled =
            new SettingItemStruct<bool>("IsUrlAutoEscapeEnabled", false);

        public static readonly SettingItemStruct<bool> WarnAmending =
            new SettingItemStruct<bool>("WarnAmending", true);

        public static readonly SettingItemStruct<bool> SuppressTagBindingInReply =
            new SettingItemStruct<bool>("SuppresTagBindingInReply", true);

        public static readonly SettingItemStruct<bool> WarnReplyFromThirdAccount =
            new SettingItemStruct<bool>("WarnReplyFromThirdAccount", true);

        public static readonly SettingItemStruct<TweetBoxClosingAction> TweetBoxClosingAction =
            new SettingItemStruct<TweetBoxClosingAction>("TweetBoxClosingAction", Settings.TweetBoxClosingAction.Confirm);

        public static readonly SettingItemStruct<bool> IsBacktrackFallback =
            new SettingItemStruct<bool>("IsBacktrackFallback", true);

        public static readonly SettingItemStruct<bool> RestorePreviousStashed =
            new SettingItemStruct<bool>("RestorePreviousStashed", false);

        public static readonly SettingItemStruct<bool> ShowMessageOnTweetFailed =
            new SettingItemStruct<bool>("ShowMessageOnTweetFailed", true);

        public static readonly SettingItemStruct<bool> CheckDesktopHeap =
            new SettingItemStruct<bool>("CheckDesktopHeap", true);

        #endregion

        #region Outer and Third Party services

        public static readonly SettingItem<string> ExternalBrowserPath =
            new SettingItem<string>("ExternalBrowserPath", null);

        public static readonly SettingItem<string> FavstarApiKey =
            new SettingItem<string>("FavstarApiKey", null);

        #endregion

        #region Notification and Confirmations

        public static readonly SettingItemStruct<bool> ConfirmOnExitApp =
            new SettingItemStruct<bool>("ConfirmOnExitApp", true);

        #endregion

        #region High-level configurations

        public static readonly SettingItemStruct<bool> AcceptUnstableVersion =
                    new SettingItemStruct<bool>("AcceptUnstableVersion", App.IsUnstableVersion);

        public static readonly SettingItem<string> UserAgent =
            new SettingItem<string>("UserAgent", null);

        #region Web proxy configuration

        public static readonly SettingItemStruct<WebProxyConfiguration> WebProxy =
            new SettingItemStruct<WebProxyConfiguration>("WebProxy", WebProxyConfiguration.Default);

        public static readonly SettingItem<string> WebProxyHost =
            new SettingItem<string>("WebProxyHost", String.Empty);

        public static readonly SettingItemStruct<int> WebProxyPort =
            new SettingItemStruct<int>("WebProxyPort", 0);

        public static readonly SettingItemStruct<bool> BypassWebProxyInLocal =
            new SettingItemStruct<bool>("BypassWebProxyInLocal", false);

        public static readonly SettingItem<string[]> WebProxyBypassList =
            new SettingItem<string[]>("WebProxyBypassList", null);

        #endregion

        public static readonly SettingItem<string> ApiProxy =
            new SettingItem<string>("ApiProxy", null);

        public static readonly SettingItemStruct<bool> LoadUnsafePlugins =
            new SettingItemStruct<bool>("LoadUnsafePlugins", false);

        public static readonly SettingItemStruct<bool> LoadPluginFromDevFolder =
            new SettingItemStruct<bool>("LoadPluginFromDevFolder", false);

        public static readonly SettingItemStruct<bool> DisableGeoLocationService =
            new SettingItemStruct<bool>("DisableGeoLocationService", false);

        public static readonly SettingItemStruct<int> EventDisplayMinimumMSec =
            new SettingItemStruct<int>("EventDisplayMinimumMSec", 200);

        public static readonly SettingItemStruct<int> EventDisplayMaximumMSec =
            new SettingItemStruct<int>("EventDisplayMaximumMSec", 3000);

        public static readonly SettingItemStruct<int> UserInfoReceivePeriod =
            new SettingItemStruct<int>("UserInfoReceivePeriod", 600);

        public static readonly SettingItemStruct<int> UserRelationReceivePeriod =
            new SettingItemStruct<int>("UserRelationReceivePeriod", 5400);

        public static readonly SettingItemStruct<int> RESTReceivePeriod =
            new SettingItemStruct<int>("RESTReceivePeriod", 90);

        public static readonly SettingItemStruct<int> RESTSearchReceivePeriod =
            new SettingItemStruct<int>("RESTSearchReceivePeriod", 90);

        public static readonly SettingItemStruct<int> ListReceivePeriod =
            new SettingItemStruct<int>("ListReceivePeriod", 90);

        public static readonly SettingItemStruct<int> PostWindowTimeSec =
            new SettingItemStruct<int>("PostWindowTimeSec", 10800);

        public static readonly SettingItemStruct<int> PostLimitPerWindow =
            new SettingItemStruct<int>("PostLimitPerWindow", 128);

        #endregion

        #region Krile internal state

        public static readonly SettingItemStruct<Rect> LastWindowPosition =
            new SettingItemStruct<Rect>("LastWindowPosition", Rect.Empty);

        public static readonly SettingItemStruct<WindowState> LastWindowState =
            new SettingItemStruct<WindowState>("LastWindowState", WindowState.Normal);

        public static readonly SettingItem<string> LastImageOpenDir =
            new SettingItem<string>("LastImageOpenDir", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

        public static readonly SettingItemStruct<int> SettingVersion =
            new SettingItemStruct<int>("SettingVersion", 1);

        #endregion

        #region Easter egg

        public static readonly SettingItemStruct<bool> RotateWindowContent =
            new SettingItemStruct<bool>("RotateWindowContent", false);

        #endregion

        #region Setting infrastructure

        public class FilterSettingItem
        {
            private readonly bool _autoSave;
            private readonly string _name;
            private Func<TwitterStatus, bool> _evaluatorCache;

            private FilterExpressionRoot _expression;

            public FilterSettingItem(string name, bool autoSave = true)
            {
                _name = name;
                _autoSave = autoSave;
            }

            public string Name
            {
                get { return _name; }
            }

            public FilterExpressionRoot Value
            {
                get
                {
                    if (_expression != null) return _expression;

                    object value;
                    if (_settingValueHolder.TryGetValue(Name, out value))
                    {
                        try
                        {
                            return _expression = QueryCompiler.CompileFilters(value as string);
                        }
                        catch (FilterQueryException)
                        {
                        }
                    }
                    // return empty
                    return _expression = FilterExpressionRoot.GetEmpty(false);
                }
                set
                {
                    _expression = value;
                    _settingValueHolder[Name] = value.ToQuery();
                    _evaluatorCache = null;
                    if (_autoSave)
                    {
                        Save();
                    }
                    this.ValueChanged.SafeInvoke(value);
                }
            }

            public void AddPredicate(FilterExpressionBase expr)
            {
                var eq = expr.ToQuery();
                if (eq.StartsWith("where "))
                {
                    eq = eq.Substring(6);
                }
                AddPredicate(eq);
            }

            public void AddPredicate(string query)
            {
                Value = QueryCompiler.CompileFilters(Value.ToQuery() + " | " + query);
            }

            public Func<TwitterStatus, bool> Evaluator
            {
                get { return _evaluatorCache ?? (_evaluatorCache = Value.GetEvaluator()); }
            }

            public event Action<FilterExpressionBase> ValueChanged;
        }

        public abstract class SettingItemBase<T>
        {
            private readonly string _name;

            public SettingItemBase(string name)
            {
                this._name = name;
            }

            public string Name { get { return _name; } }

            public abstract T Value { get; set; }

            public event Action<T> ValueChanged;

            protected void RaiseValueChanged(T value)
            {
                ValueChanged.SafeInvoke(value);
            }

            public IDisposable ListenValueChanged(Action<T> listener)
            {
                return Observable.FromEvent<T>(
                    h => ValueChanged += h,
                    h => ValueChanged -= h)
                                 .Subscribe(listener);
            }
        }

        public class SettingItem<T> : SettingItemBase<T> where T : class
        {
            private readonly bool _autoSave;
            private readonly T _defaultValue;

            private T _valueCache;

            public SettingItem(string name, T defaultValue, bool autoSave = true)
                : base(name)
            {
                _defaultValue = defaultValue;
                _autoSave = autoSave;
            }

            public override T Value
            {
                get
                {
                    if (_valueCache != null) return _valueCache;

                    object value;
                    if (_settingValueHolder.TryGetValue(Name, out value))
                    {
                        return _valueCache = value as T;
                    }
                    return _valueCache = _defaultValue;
                }
                set
                {
                    _valueCache = value;
                    _settingValueHolder[Name] = value;
                    if (_autoSave)
                    {
                        Save();
                    }
                    RaiseValueChanged(value);
                }
            }
        }

        public class SettingItemStruct<T> : SettingItemBase<T> where T : struct
        {
            private readonly bool _autoSave;
            private readonly T _defaultValue;

            private T? _valueCache;

            public SettingItemStruct(string name, T defaultValue, bool autoSave = true)
                : base(name)
            {
                _defaultValue = defaultValue;
                _autoSave = autoSave;
            }

            public override T Value
            {
                get
                {
                    if (_valueCache != null) return _valueCache.Value;
                    object value;
                    return _settingValueHolder.TryGetValue(this.Name, out value)
                               ? (this._valueCache = (T)value).Value
                               : (this._valueCache = this._defaultValue).Value;
                }
                set
                {
                    _valueCache = value;
                    _settingValueHolder[Name] = value;
                    if (_autoSave)
                    {
                        Save();
                    }
                    RaiseValueChanged(value);
                }
            }
        }

        #endregion

        /// <summary>
        /// The flag identifies newbies of Krile.
        /// </summary>
        public static bool IsFirstGenerated { get; private set; }

        public static bool IsLoaded { get; private set; }

        public static bool LoadSettings()
        {
            IsFirstGenerated = false;
            if (File.Exists(App.ConfigurationFilePath))
            {
                try
                {
                    using (var fs = File.Open(App.ConfigurationFilePath, FileMode.Open, FileAccess.Read))
                    {
                        _settingValueHolder = new ConcurrentDictionary<string, object>(
                            XamlServices.Load(fs) as IDictionary<string, object> ?? new Dictionary<string, object>());
                    }
                    _manager = new AccountManager(_accounts);
                    IsLoaded = true;
                    return true;
                }
                catch (Exception ex)
                {
                    var option = new TaskDialogOptions
                    {
                        Title = "Krile 設定読み込みエラー",
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "設定が破損しています。",
                        Content = "設定ファイルに異常があるため、読み込めませんでした。" + Environment.NewLine +
                                  "どのような操作を行うか選択してください。",
                        ExpandedInfo = ex.ToString(),
                        CommandButtons = new[] { "設定を初期化", "バックアップを作成し初期化", "Krileを終了" },
                    };
                    var result = TaskDialog.Show(option);
                    if (!result.CommandButtonResult.HasValue ||
                        result.CommandButtonResult.Value == 2)
                    {
                        // shutdown
                        return false;
                    }
                    if (result.CommandButtonResult == 1)
                    {
                        try
                        {
                            var cpfn = "Krile_CorruptedConfig_" + Path.GetRandomFileName() + ".xml";
                            File.Copy(App.ConfigurationFilePath, Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                                cpfn));
                        }
                        catch (Exception iex)
                        {
                            var noption = new TaskDialogOptions
                            {
                                Title = "バックアップ失敗",
                                MainIcon = VistaTaskDialogIcon.Error,
                                MainInstruction = "何らかの原因により、バックアップが正常に行えませんでした。",
                                Content = "これ以上の動作を継続できません。",
                                ExpandedInfo = iex.ToString(),
                                CommandButtons = new[] { "Krileを終了" }
                            };
                            TaskDialog.Show(noption);
                            return false;
                        }
                    }
                    File.Delete(App.ConfigurationFilePath);
                    _settingValueHolder = new ConcurrentDictionary<string, object>();
                    _manager = new AccountManager(_accounts);
                    IsLoaded = true;
                    return true;
                }
            }
            _settingValueHolder = new ConcurrentDictionary<string, object>();
            IsFirstGenerated = true;
            _manager = new AccountManager(_accounts);
            IsLoaded = true;
            return true;
        }

        public static void Save()
        {
            // save before load cause loss the setting!
            if (!IsLoaded) return;
            lock (_settingValueHolder)
            {
                try
                {
                    using (var fs = File.Open(App.ConfigurationFilePath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        // sort by key
                        var sd = new SortedDictionary<string, object>(_settingValueHolder);
                        XamlServices.Save(fs, sd);
                    }
                }
                catch (IOException)
                {
                    // file error.
                }
                catch (UnauthorizedAccessException)
                {
                    // could not write.
                }
            }
        }
    }

    public enum ScrollLockStrategy
    {
        /// <summary>
        ///     Always unlock
        /// </summary>
        None,

        /// <summary>
        ///     Always scroll locked
        /// </summary>
        Always,

        /// <summary>
        ///     Change lock/unlock explicitly
        /// </summary>
        Explicit,

        /// <summary>
        ///     If scrolled (scroll offset != 0)
        /// </summary>
        WhenScrolled,

        /// <summary>
        ///     When mouse over
        /// </summary>
        WhenMouseOver,
    }

    public enum TweetBoxClosingAction
    {
        Confirm,
        SaveToDraft,
        Discard,
    }

    public enum ImageUploaderService
    {
        TwitterOfficial,
        TwitPic,
        YFrog,
    }

    public enum WebProxyConfiguration
    {
        Default,
        None,
        Custom
    }
}