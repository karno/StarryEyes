using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Imaging;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using TaskDialogInterop;

namespace StarryEyes.Settings
{
    public static class Setting
    {
        static SortedDictionary<string, object> settingValueHolder;

        public static bool IsFirstGenerated { get; private set; }

        public static bool LoadSettings()
        {
            IsFirstGenerated = false;
            if (File.Exists(App.ConfigurationFilePath))
            {
                try
                {
                    using (var fs = File.Open(App.ConfigurationFilePath, FileMode.Open, FileAccess.Read))
                    {
                        settingValueHolder = new SortedDictionary<string, object>(
                            XamlServices.Load(fs) as IDictionary<string, object>);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    var option = new TaskDialogOptions()
                    {
                        MainIcon = VistaTaskDialogIcon.Error,
                        Title = "Krile 設定読み込みエラー",
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
                    else if (result.CommandButtonResult == 1)
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
                            var noption = new TaskDialogOptions()
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
                    settingValueHolder = new SortedDictionary<string, object>();
                    return true;
                }
            }
            else
            {
                settingValueHolder = new SortedDictionary<string, object>();
                IsFirstGenerated = true;
                return true;
            }
        }

        public static void Save()
        {
            using (var fs = File.Open(App.ConfigurationFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                XamlServices.Save(fs, settingValueHolder);
            }
        }

        public static void Clear()
        {
            Properties.Settings.Default.Reset();
        }

        public static readonly SettingItemStruct<bool> IsPowerUser =
            new SettingItemStruct<bool>("IsPowerUser", false);

        #region Authentication and accounts

        private static readonly SettingItem<List<AccountSetting>> accounts =
            new SettingItem<List<AccountSetting>>("Accounts", new List<AccountSetting>());

        internal static IEnumerable<AccountSetting> _AccountsInternalDataStore
        {
            get { return accounts.Value; }
            set { accounts.Value = value.ToList(); }
        }

        public static readonly SettingItem<string> GlobalConsumerKey =
            new SettingItem<string>("GlobalConsumerKey", null);

        public static readonly SettingItem<string> GlobalConsumerSecret =
            new SettingItem<string>("GlobalConsumerSecret", null);

        public static readonly SettingItemStruct<bool> IsBacktrackFallback =
             new SettingItemStruct<bool>("IsBacktrackFallback", true);

        #endregion

        #region Timeline display and action

        public static readonly FilterSettingItem Muteds = new FilterSettingItem("Muteds");

        public static readonly SettingItemStruct<bool> AllowFavoriteMyself =
            new SettingItemStruct<bool>("AllowFavoriteMyself", false);

        public static readonly SettingItemStruct<ScrollLockStrategy> ScrollLockStrategy =
            new SettingItemStruct<ScrollLockStrategy>("ScrollLockStrategy", Settings.ScrollLockStrategy.WhenScrolled);

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

        #endregion

        #region Outer and Third Party services

        public static readonly SettingItem<string> ExternalBrowserPath =
            new SettingItem<string>("ExternalBrowserPath", null);

        private static readonly SettingItemStruct<int> imageUploaderService =
            new SettingItemStruct<int>("ImageUploaderService", 0);

        public static ImageUploaderService ImageUploaderService
        {
            get { return (ImageUploaderService)imageUploaderService.Value; }
            set { imageUploaderService.Value = (int)value; }
        }

        public static ImageUploaderBase GetImageUploader()
        {
            switch (ImageUploaderService)
            {
                case Settings.ImageUploaderService.TwitPic:
                    return new TwitPicUploader();
                case Settings.ImageUploaderService.YFrog:
                    return new YFrogUploader();
                case Settings.ImageUploaderService.TwitterOfficial:
                default:
                    return new TwitterPhotoUploader();
            }
        }

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

        public static readonly SettingItemStruct<int> EventDispatchMinimumMSec =
            new SettingItemStruct<int>("EventDispatchMinimumMSec", 200);

        public static readonly SettingItemStruct<int> EventDispatchMaximumMSec =
            new SettingItemStruct<int>("EventDispatchMaximumMSec", 3000);

        public static readonly SettingItemStruct<int> UserInfoReceivePeriod =
            new SettingItemStruct<int>("UserInfoReceivePeriod", 600);

        public static readonly SettingItemStruct<int> RESTReceivePeriod =
            new SettingItemStruct<int>("RESTReceivePeriod", 90);

        public static readonly SettingItemStruct<int> PostWindowTimeSec =
            new SettingItemStruct<int>("PostWindowTimeSec", 10800);

        public static readonly SettingItemStruct<int> PostLimitPerWindow =
            new SettingItemStruct<int>("PostLimitPerWindow", 128);

        #endregion

        #region Krile internal state

        public static readonly SettingItem<string> LastImageOpenDir =
            new SettingItem<string>("LastImageOpenDir", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

        #endregion

        #region Setting infrastructure

        public class FilterSettingItem
        {
            public event Action<FilterExpressionBase> OnValueChanged;

            private string _name;
            public string Name
            {
                get { return _name; }
            }

            private bool _autoSave;
            public FilterSettingItem(string name, bool autoSave = true)
            {
                this._name = name;
                this._autoSave = autoSave;
            }

            private FilterExpressionRoot _expression;
            public FilterExpressionRoot Value
            {
                get
                {
                    try
                    {
                        return _expression ??
                            (_expression = QueryCompiler.CompileFilters(settingValueHolder[Name] as string));
                    }
                    catch
                    {
                        return new FilterExpressionRoot();
                    }
                }
                set
                {
                    _expression = value;
                    settingValueHolder[Name] = value.ToQuery();
                    _evaluatorCache = null;
                    if (_autoSave)
                    {
                        Save();
                    }
                    var handler = OnValueChanged;
                    if (handler != null)
                        OnValueChanged(value);
                }
            }

            private Func<TwitterStatus, bool> _evaluatorCache = null;
            public Func<TwitterStatus, bool> Evaluator
            {
                get { return _evaluatorCache ?? (_evaluatorCache = Value.GetEvaluator()); }
            }
        }

        public class SettingItem<T> where T : class
        {
            public event Action<T> OnValueChanged;

            private string _name;
            public string Name
            {
                get { return _name; }
            }

            private T _defaultValue;
            private bool _autoSave;
            public SettingItem(string name, T defaultValue, bool autoSave = true)
            {
                this._name = name;
                this._defaultValue = defaultValue;
                this._autoSave = autoSave;
            }

            private T valueCache;
            public T Value
            {
                get
                {
                    try
                    {
                        return valueCache ??
                            (valueCache = settingValueHolder[Name] as T);
                    }
                    catch
                    {
                        return valueCache = _defaultValue;
                    }
                }
                set
                {
                    valueCache = value;
                    settingValueHolder[Name] = value;
                    if (_autoSave)
                    {
                        Save();
                    }
                    var handler = OnValueChanged;
                    if (handler != null)
                        OnValueChanged(value);
                }
            }
        }

        public class SettingItemStruct<T> where T : struct
        {
            public event Action<T> OnValueChanged;

            private string _name;
            public string Name
            {
                get { return _name; }
            }

            private T _defaultValue;
            private bool _autoSave;
            public SettingItemStruct(string name, T defaultValue, bool autoSave = true)
            {
                this._name = name;
                this._defaultValue = defaultValue;
                this._autoSave = autoSave;
            }

            private T? valueCache;
            public T Value
            {
                get
                {
                    try
                    {
                        return valueCache ??
                            (valueCache = (T)settingValueHolder[Name]).Value;
                    }
                    catch
                    {
                        return (valueCache = _defaultValue).Value;
                    }
                }
                set
                {
                    valueCache = value;
                    settingValueHolder[Name] = value;
                    if (_autoSave)
                    {
                        Save();
                    }
                    var handler = OnValueChanged;
                    if (handler != null)
                        OnValueChanged(value);
                }
            }
        }

        #endregion
    }

    public enum ScrollLockStrategy
    {
        /// <summary>
        /// Always unlock
        /// </summary>
        None,
        /// <summary>
        /// Always scroll locked
        /// </summary>
        Always,
        /// <summary>
        /// Change lock/unlock explicitly
        /// </summary>
        Explicit,
        /// <summary>
        /// If scrolled (scroll offset != 0)
        /// </summary>
        WhenScrolled,
        /// <summary>
        /// When mouse over
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
