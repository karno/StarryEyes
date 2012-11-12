using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Imaging;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Hub;

namespace StarryEyes.Settings
{
    public static class Setting
    {
        static SortedDictionary<string, object> settingValueHolder;

        public static void LoadSettings()
        {
            if (File.Exists(App.ConfigurationFilePath))
            {
                try
                {
                    using (var fs = File.Open(App.ConfigurationFilePath, FileMode.Open, FileAccess.Read))
                    {
                        settingValueHolder = new SortedDictionary<string, object>(
                            XamlServices.Load(fs) as IDictionary<string, object>);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        var cpfn = "Krile_CorruptedConfig_" + Path.GetRandomFileName() + ".xml";
                        File.Copy(App.ConfigurationFilePath, Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                            cpfn));
                        File.Delete(App.ConfigurationFilePath);
                        settingValueHolder = new SortedDictionary<string, object>();
                        AppInformationHub.PublishInformation(new AppInformation(AppInformationKind.Warning,
                            "SETTING_LOAD_ERROR",
                            "設定が読み込めません。バックアップをデスクトップに作成し、設定を初期化しました。",
                            "バックアップ設定ファイルは " + cpfn + " として作成されました。" + Environment.NewLine +
                            "あなた自身で原因の特定と修復が可能な場合は、修復した設定ファイルを元のディレクトリに配置し直すことで設定を回復できるかもしれません。" + Environment.NewLine +
                            "元のディレクトリ: " + App.ConfigurationFilePath + Environment.NewLine +
                            "送出された例外: " + ex.ToString()));
                    }
                    catch (Exception ex_)
                    {
                        Environment.FailFast(
                            "設定ファイルの緊急バックアップ ストアが行えませんでした。", ex_);
                        return;
                    }
                }
            }
            else
            {
                settingValueHolder = new SortedDictionary<string, object>();
            }
        }

        public static void Save()
        {
            using (var fs = File.Open(App.ConfigurationFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
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

        public static FilterSettingItem Muteds = new FilterSettingItem("Muteds");

        public static readonly SettingItemStruct<bool> AllowFavoriteMyself = new SettingItemStruct<bool>("AllowFavoriteMyself", false);

        #endregion

        #region Input control

        public static readonly SettingItemStruct<bool> IsUrlAutoEscapeEnabled = new SettingItemStruct<bool>("IsUrlAutoEscapeEnabled", false);

        public static readonly SettingItemStruct<bool> IsWarnAmendTweet = new SettingItemStruct<bool>("IsWarnAmendTweet", true);

        public static readonly SettingItemStruct<bool> IsWarnReplyFromThirdAccount = new SettingItemStruct<bool>("IsWarnReplyFromThirdAccount", true);

        public static readonly SettingItemStruct<TweetBoxClosingAction> TweetBoxClosingAction = new SettingItemStruct<TweetBoxClosingAction>("TweetBoxClosingAction", Settings.TweetBoxClosingAction.Confirm);

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

        #region High-level configurations

        public static readonly SettingItemStruct<bool> ApplyMuteToRetweetOriginals = new SettingItemStruct<bool>("ApplyMuteToRetweetOriginals", true);

        public static readonly SettingItemStruct<int> EventDispatchMinimumMSec = new SettingItemStruct<int>("EventDispatchMinimumMSec", 200);

        public static readonly SettingItemStruct<int> EventDispatchMaximumMSec = new SettingItemStruct<int>("EventDispatchMaximumMSec", 3000);

        #endregion

        #region Krile internal state

        public static readonly SettingItem<string> LastImageOpenDir = new SettingItem<string>("LastImageOpenDir", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

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
                get { return _evaluatorCache ?? (_evaluatorCache = Value.GetEvaluator());}
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
