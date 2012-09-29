using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;
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
                        settingValueHolder = new SortedDictionary<string, object>();
                        InformationHub.PublishInformation(new Information(InformationKind.Warning,
                            "SETTING_LOAD_ERROR",
                            "設定が読み込めません。バックアップをデスクトップに作成し、設定を初期化しました。",
                            "バックアップ設定ファイルは " + cpfn + " として作成されました。" + Environment.NewLine +
                            "あなた自身で原因の特定と修復が可能な場合は、修復した設定ファイルを元のディレクトリに配置し直すことで設定を回復できるかもしれません。" + Environment.NewLine +
                            "元のディレクトリ: " + App.ConfigurationFilePath + Environment.NewLine +
                            "送出された例外: " + ex.ToString()));
                    }
                    catch(Exception ex_)
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

        public static SettingItemStruct<bool> IsPowerUser = new SettingItemStruct<bool>("IsPowerUser", false);

        public static SettingItem<string> ExternalBrowserPath = new SettingItem<string>("ExternalBrowserPath", null);

        private static SettingItem<List<AccountSetting>> accounts = new SettingItem<List<AccountSetting>>("Accounts", new List<AccountSetting>());

        public static IEnumerable<AccountSetting> Accounts
        {
            get { return accounts.Value; }
            set { accounts.Value = value.ToList(); }
        }

        public static AccountSetting LookupAccountSetting(long id)
        {
            return accounts.Value.Where(a => a.UserId == id).FirstOrDefault();
        }

        #region Timeline display and action

        public static SettingItemStruct<bool> AllowFavoriteMyself = new SettingItemStruct<bool>("AllowFavoriteMyself", false);

        #endregion

        #region Setting infrastructure

        public class SettingItem<T> where T : class
        {
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
                        return _defaultValue;
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
                }
            }
        }

        public class SettingItemStruct<T> where T : struct
        {
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
                        return _defaultValue;
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
                }
            }
        }

        #endregion
    }

}
