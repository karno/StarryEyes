using System.Collections.Generic;
using System.Configuration;

namespace StarryEyes.Mystique.Settings
{
    public static class Setting
    {
        static Setting()
        {
            Properties.Settings.Default.Upgrade();
        }

        public static void Clear()
        {
            Properties.Settings.Default.Reset();
        }

        public static SettingItemStruct<bool> IsPowerUser = new SettingItemStruct<bool>("IsPowerUser", false);

        public static SettingItem<string> ExternalBrowserPath = new SettingItem<string>("ExternalBrowserPath", null);

        public static SettingItem<List<AccountSetting>> Accounts =
            new SettingItem<List<AccountSetting>>("Accounts", new List<AccountSetting>());
    }

    public class SettingItem<T> where T : class
    {
        private string _name;
        public string Name
        {
            get { return _name; }
        }

        private T _defaultValue;
        public SettingItem(string name, T defaultValue)
        {
            this._name = name;
            this._defaultValue = defaultValue;
        }

        private T valueCache;
        public T Value
        {
            get
            {
                try
                {
                    return valueCache ??
                        (valueCache = Properties.Settings.Default[Name] as T);
                }
                catch (SettingsPropertyNotFoundException)
                {
                    return _defaultValue;
                }
            }
            set
            {
                valueCache = value;
                Properties.Settings.Default[Name] = value;
                Properties.Settings.Default.Save();
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
        public SettingItemStruct(string name, T defaultValue)
        {
            this._name = name;
            this._defaultValue = defaultValue;
        }

        private T? valueCache;
        public T Value
        {
            get
            {
                try
                {
                    return valueCache ??
                        (valueCache = (T)Properties.Settings.Default[Name]).Value;
                }
                catch (SettingsPropertyNotFoundException)
                {
                    return _defaultValue;
                }
            }
            set
            {
                valueCache = value;
                Properties.Settings.Default[Name] = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
