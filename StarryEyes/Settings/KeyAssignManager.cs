using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Settings.KeyAssigns;

namespace StarryEyes.Settings
{
    /// <summary>
    /// Manage Key Binding profiles.
    /// </summary>
    public static class KeyAssignManager
    {
        private static readonly SortedDictionary<string, KeyAssignProfile> Profiles =
            new SortedDictionary<string, KeyAssignProfile>();

        private static readonly SortedDictionary<string, KeyAssignAction> Actions =
            new SortedDictionary<string, KeyAssignAction>();

        public static string KeyAssignsProfileDirectoryPath
        {
            get { return Path.Combine(App.ConfigurationDirectoryPath, App.KeyAssignProfilesDirectory); }
        }

        internal static void Initialize()
        {
            var path = KeyAssignsProfileDirectoryPath;

            // make sure existing directories.
            Directory.CreateDirectory(KeyAssignsProfileDirectoryPath);

            // load all assigns.
            foreach (var file in Directory.EnumerateFiles(path, "*.txt"))
            {
                Load(file);
            }
            CheckSetting();
            // listen setting changed
            Setting.KeyAssign.OnValueChanged += _ => RaiseKeyAssignChanged();
        }

        private static void Load(string file)
        {
            if (!File.Exists(file)) return;
            try
            {
                var profile = KeyAssignProfile.FromFile(file);
                Profiles[profile.Name] = profile;
            }
            catch (Exception ex)
            {
                BackpanelModel.RegisterEvent(new KeyAssignCouldNotLoadEvent(file, ex, () => Load(file)));
            }
        }

        private static void CheckSetting()
        {
            // check assign is existed
            var group = Setting.KeyAssign.Value ?? DefaultAssignProvider.DefaultAssignName;
            if (!Profiles.ContainsKey(group))
            {
                // load default
                Setting.KeyAssign.Value = DefaultAssignProvider.DefaultAssignName;
                if (!Profiles.ContainsKey(DefaultAssignProvider.DefaultAssignName))
                {
                    // default binding is not found
                    // make default
                    var defbind = DefaultAssignProvider.GetDefault();
                    defbind.Save(KeyAssignsProfileDirectoryPath);
                    Profiles.Add(defbind.Name, defbind);
                }
            }
        }

        public static KeyAssignProfile CurrentProfile
        {
            get
            {
                var profileId = Setting.KeyAssign.Value ?? DefaultAssignProvider.DefaultAssignName;
                if (Profiles.ContainsKey(profileId))
                {
                    return Profiles[profileId];
                }

                // not found
                BackpanelModel.RegisterEvent(new KeyAssignProfileNotFoundEvent(profileId));
                return DefaultAssignProvider.GetEmpty();
            }
        }

        public static void RegisterActions(params KeyAssignAction[] callbacks)
        {
            callbacks.ForEach(RegisterAction);
        }

        public static void RegisterAction(KeyAssignAction callback)
        {
            Actions[callback.Name] = callback;
        }

        public static bool InvokeAction(KeyAssignActionDescription desc)
        {
            KeyAssignAction callback;
            if (Actions.TryGetValue(desc.ActionName, out callback))
            {
                callback.Invoke(desc.Argument);
                return true;
            }
            return false;
        }

        public static event Action OnKeyAssignChanged;

        private static void RaiseKeyAssignChanged()
        {
            Action handler = OnKeyAssignChanged;
            if (handler != null) handler();
        }
    }

    public sealed class KeyAssignAction
    {
        public static KeyAssignAction Create(string name, Action action)
        {
            return new KeyAssignAction(name, _ => action(), false);
        }

        public static KeyAssignAction CreateWithArgumentRequired(string name, Action<string> action)
        {
            return new KeyAssignAction(name, action, true);
        }

        public static KeyAssignAction CreateWithArgumentOptional(string name, Action<string> action)
        {
            return new KeyAssignAction(name, action);
        }

        public KeyAssignAction(string name, Action<string> callback, bool? hasArgument = null)
        {
            _name = name;
            _callback = callback;
            _hasArgument = hasArgument;
        }

        private readonly string _name;
        public string Name
        {
            get { return _name; }
        }

        private readonly Action<string> _callback;

        private readonly bool? _hasArgument;

        public void Invoke(string argument)
        {
            if (_hasArgument != null && _hasArgument.Value == !String.IsNullOrEmpty(argument))
            {
                BackpanelModel.RegisterEvent(
                    new OperationFailedEvent(
                        String.Format(_hasArgument.Value
                                          ? "キーバインド {0} には引数の指定が必要です。"
                                          : "キーバインド {0} に引数を指定することはできません。", Name)));
            }
            else
            {
                _callback(argument);
            }
        }
    }
}
