using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace StarryEyes.Mystique.Kernel
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
    }

    public class SettingItem<T> where T : class
    {
        private string _name;
        public string Name
        {
            get { return _name; }
        }

        public SettingItem(string name, T defaultValue)
        {
            this._name = name;
        }

        private T valueCache;
        public T Value
        {
            get
            {
                return valueCache ??
                    (valueCache = Properties.Settings.Default[Name] as T);
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

        public SettingItemStruct(string name, T defaultValue)
        {
            this._name = name;
        }

        private T? valueCache;
        public T Value
        {
            get
            {
                return valueCache ??
                    (valueCache = (T)Properties.Settings.Default[Name]).Value;
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
