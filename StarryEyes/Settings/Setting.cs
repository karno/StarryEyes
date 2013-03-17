using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Imaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Tab;
using TaskDialogInterop;

namespace StarryEyes.Settings
{
    public static class Setting
    {
        private static SortedDictionary<string, object> _settingValueHolder;

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

        private static readonly SettingItem<List<AccountSetting>> _accounts =
            new SettingItem<List<AccountSetting>>("Accounts", new List<AccountSetting>());

        public static readonly SettingItem<string> GlobalConsumerKey =
            new SettingItem<string>("GlobalConsumerKey", null);

        public static readonly SettingItem<string> GlobalConsumerSecret =
            new SettingItem<string>("GlobalConsumerSecret", null);

        public static readonly SettingItemStruct<bool> IsBacktrackFallback =
            new SettingItemStruct<bool>("IsBacktrackFallback", true);

        // ReSharper disable InconsistentNaming
        internal static IEnumerable<AccountSetting> @Infrastructure_Accounts
        {
            get { return _accounts.Value; }
            set { _accounts.Value = value.ToList(); }
        }
        // ReSharper restore InconsistentNaming

        #endregion

        #region Timeline display and action

        public static readonly FilterSettingItem Muteds = new FilterSettingItem("Muteds");

        public static readonly SettingItemStruct<bool> AllowFavoriteMyself =
            new SettingItemStruct<bool>("AllowFavoriteMyself", false);

        public static readonly SettingItemStruct<ScrollLockStrategy> ScrollLockStrategy =
            new SettingItemStruct<ScrollLockStrategy>("ScrollLockStrategy", Settings.ScrollLockStrategy.WhenScrolled);

        public static readonly SettingItem<string> KeyAssign =
            new SettingItem<string>("KeyAssign", null);

        #endregion

        #region Input control

        public static readonly SettingItemStruct<bool> IsUrlAutoEscapeEnabled =
            new SettingItemStruct<bool>("IsUrlAutoEscapeEnabled", false);

        public static readonly SettingItemStruct<bool> IsWarnAmendTweet =
            new SettingItemStruct<bool>("IsWarnAmendTweet", true);

        public static readonly SettingItemStruct<bool> IsWarnReplyFromThirdAccount =
            new SettingItemStruct<bool>("IsWarnReplyFromThirdAccount", true);

        public static readonly SettingItemStruct<TweetBoxClosingAction> TweetBoxClosingAction =
            new SettingItemStruct<TweetBoxClosingAction>("TweetBoxClosingAction", Settings.TweetBoxClosingAction.Confirm);

        public static readonly SettingItemStruct<bool> RestorePreviousStashed =
            new SettingItemStruct<bool>("RestorePreviousStashed", false);

        #endregion

        #region Outer and Third Party services

        public static readonly SettingItem<string> ExternalBrowserPath =
            new SettingItem<string>("ExternalBrowserPath", null);

        private static readonly SettingItemStruct<int> _imageUploaderService =
            new SettingItemStruct<int>("ImageUploaderService", 0);

        public static ImageUploaderService ImageUploaderService
        {
            get { return (ImageUploaderService)_imageUploaderService.Value; }
            set { _imageUploaderService.Value = (int)value; }
        }

        public static ImageUploaderBase GetImageUploader()
        {
            switch (ImageUploaderService)
            {
                case ImageUploaderService.TwitPic:
                    return new TwitPicUploader();
                case ImageUploaderService.YFrog:
                    return new YFrogUploader();
                default:
                    return new TwitterPhotoUploader();
            }
        }

        public static readonly SettingItem<string> FavstarApiKey =
            new SettingItem<string>("FavstarApiKey", null);

        #endregion

        #region Notification and Confirmations

        public static readonly SettingItemStruct<bool> ConfirmOnExitApp =
            new SettingItemStruct<bool>("ConfirmOnExitApp", true);

        #endregion

        #region High-level configurations

        public static readonly SettingItem<string> UserAgent =
            new SettingItem<string>("UserAgent", "Krile StarryEyes/Breezy TL with ReactiveOAuth");

        public static readonly SettingItemStruct<bool> LoadUnsafePlugins =
            new SettingItemStruct<bool>("LoadUnsafePlugins", false);

        public static readonly SettingItemStruct<bool> LoadPluginFromDevFolder =
            new SettingItemStruct<bool>("LoadPluginFromDevFolder", false);

        public static readonly SettingItemStruct<bool> ApplyMuteToRetweetOriginals =
            new SettingItemStruct<bool>("ApplyMuteToRetweetOriginals", true);

        public static readonly SettingItemStruct<int> EventDisplayMinimumMSec =
            new SettingItemStruct<int>("EventDisplayMinimumMSec", 200);

        public static readonly SettingItemStruct<int> EventDisplayMaximumMSec =
            new SettingItemStruct<int>("EventDisplayMaximumMSec", 3000);

        public static readonly SettingItemStruct<int> UserInfoReceivePeriod =
            new SettingItemStruct<int>("UserInfoReceivePeriod", 600);

        public static readonly SettingItemStruct<int> UserRelationReceivePeriod =
            new SettingItemStruct<int>("RESTReceivePeriod", 5400);

        public static readonly SettingItemStruct<int> RESTReceivePeriod =
            new SettingItemStruct<int>("RESTReceivePeriod", 90);

        public static readonly SettingItemStruct<int> PostWindowTimeSec =
            new SettingItemStruct<int>("PostWindowTimeSec", 10800);

        public static readonly SettingItemStruct<int> PostLimitPerWindow =
            new SettingItemStruct<int>("PostLimitPerWindow", 128);

        #endregion

        #region Krile internal state

        public static readonly SettingItemStruct<bool> DatabaseCorruption =
            new SettingItemStruct<bool>("DatabaseCorruption", false);

        public static readonly SettingItem<string> LastImageOpenDir =
            new SettingItem<string>("LastImageOpenDir", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

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
                    return _expression = new FilterExpressionRoot();
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
                    Action<FilterExpressionBase> handler = OnValueChanged;
                    if (handler != null)
                        OnValueChanged(value);
                }
            }

            public Func<TwitterStatus, bool> Evaluator
            {
                get { return _evaluatorCache ?? (_evaluatorCache = Value.GetEvaluator()); }
            }

            public event Action<FilterExpressionBase> OnValueChanged;
        }

        public class SettingItem<T> where T : class
        {
            private readonly bool _autoSave;
            private readonly T _defaultValue;
            private readonly string _name;

            private T _valueCache;

            public SettingItem(string name, T defaultValue, bool autoSave = true)
            {
                _name = name;
                _defaultValue = defaultValue;
                _autoSave = autoSave;
            }

            public string Name
            {
                get { return _name; }
            }

            public T Value
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
                    Action<T> handler = OnValueChanged;
                    if (handler != null)
                        OnValueChanged(value);
                }
            }

            public event Action<T> OnValueChanged;
        }

        public class SettingItemStruct<T> where T : struct
        {
            private readonly bool _autoSave;
            private readonly T _defaultValue;
            private readonly string _name;

            private T? _valueCache;

            public SettingItemStruct(string name, T defaultValue, bool autoSave = true)
            {
                _name = name;
                _defaultValue = defaultValue;
                _autoSave = autoSave;
            }

            public string Name
            {
                get { return _name; }
            }

            public T Value
            {
                get
                {
                    if (_valueCache != null) return _valueCache.Value;
                    object value;
                    if (_settingValueHolder.TryGetValue(Name, out value))
                    {
                        return (_valueCache = (T)value).Value;
                    }
                    return (_valueCache = _defaultValue).Value;
                }
                set
                {
                    _valueCache = value;
                    _settingValueHolder[Name] = value;
                    if (_autoSave)
                    {
                        Save();
                    }
                    Action<T> handler = OnValueChanged;
                    if (handler != null)
                        OnValueChanged(value);
                }
            }

            public event Action<T> OnValueChanged;
        }

        #endregion

        /// <summary>
        /// The flag identifies newbies of Krile.
        /// </summary>
        public static bool IsFirstGenerated { get; private set; }

        public static bool LoadSettings()
        {
            IsFirstGenerated = false;
            if (File.Exists(App.ConfigurationFilePath))
            {
                try
                {
                    using (FileStream fs = File.Open(App.ConfigurationFilePath, FileMode.Open, FileAccess.Read))
                    {
                        _settingValueHolder = new SortedDictionary<string, object>(
                            XamlServices.Load(fs) as IDictionary<string, object> ?? new Dictionary<string, object>());
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    var option = new TaskDialogOptions
                    {
                        MainIcon = VistaTaskDialogIcon.Error,
                        Title = "Krile 設定読み込みエラー",
                        MainInstruction = "設定が破損しています。",
                        Content = "設定ファイルに異常があるため、読み込めませんでした。" + Environment.NewLine +
                                  "どのような操作を行うか選択してください。",
                        ExpandedInfo = ex.ToString(),
                        CommandButtons = new[] { "設定を初期化", "バックアップを作成し初期化", "Krileを終了" },
                    };
                    TaskDialogResult result = TaskDialog.Show(option);
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
                            string cpfn = "Krile_CorruptedConfig_" + Path.GetRandomFileName() + ".xml";
                            File.Copy(App.ConfigurationFilePath, Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                                cpfn));
                        }
                        catch (Exception iex)
                        {
                            var noption = new TaskDialogOptions
                            {
                                Title = "バックアップ失敗",
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
                    _settingValueHolder = new SortedDictionary<string, object>();
                    return true;
                }
            }
            _settingValueHolder = new SortedDictionary<string, object>();
            IsFirstGenerated = true;
            return true;
        }

        public static void Save()
        {
            using (FileStream fs = File.Open(App.ConfigurationFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                XamlServices.Save(fs, _settingValueHolder);
            }
        }

        public static void Clear()
        {
            Properties.Settings.Default.Reset();
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
}