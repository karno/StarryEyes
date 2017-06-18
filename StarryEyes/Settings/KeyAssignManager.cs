using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings.KeyAssigns;

namespace StarryEyes.Settings
{
    /// <summary>
    /// Manage Key Binding profiles.
    /// </summary>
    public static class KeyAssignManager
    {
        private static readonly IDictionary<string, KeyAssignProfile> Profiles =
            new Dictionary<string, KeyAssignProfile>();

        private static readonly IDictionary<string, KeyAssignAction> Actions =
            new Dictionary<string, KeyAssignAction>();

        public static IEnumerable<string> LoadedProfiles
        {
            get { return Profiles.Keys; }
        }

        public static IEnumerable<KeyAssignAction> RegisteredActions
        {
            get { return Actions.Values; }
        }

        public static string KeyAssignsProfileDirectoryPath
        {
            get { return Path.Combine(App.ConfigurationDirectoryPath, App.KeyAssignProfilesDirectory); }
        }

        public static event Action KeyAssignChanged;

        internal static void Initialize()
        {
            // make sure existing directories.
            Directory.CreateDirectory(KeyAssignsProfileDirectoryPath);

            ReloadCandidates();

            // listen setting changed
            Setting.KeyAssign.ValueChanged += _ => KeyAssignChanged.SafeInvoke();
        }

        public static void ReloadCandidates()
        {
            var path = KeyAssignsProfileDirectoryPath;

            // clear loaded profiles.
            Profiles.Clear();

            // load all assigns.
            foreach (var file in Directory.EnumerateFiles(path, "*.txt"))
            {
                Load(file);
            }
            CheckSetting();
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
                MainWindowModel.ShowTaskDialog(
                    new TaskDialogOptions
                    {
                        Title = SettingModelResources.KeyAssignErrorTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = SettingModelResources.KeyAssignErrorInst,
                        Content = SettingModelResources.KeyAssignErrorContent + Environment.NewLine +
                                  file,
                        ExpandedInfo = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close
                    });
            }
        }

        private static void CheckSetting()
        {
            // check assign is existed
            var group = Setting.KeyAssign.Value ?? DefaultAssignProvider.DefaultAssignName;
            if (Profiles.ContainsKey(group)) return;
            // load default
            Setting.KeyAssign.Value = DefaultAssignProvider.DefaultAssignName;
            if (Profiles.ContainsKey(DefaultAssignProvider.DefaultAssignName)) return;
            // default binding is not found
            // make default
            var defbind = DefaultAssignProvider.GetDefault();
            defbind.Save(KeyAssignsProfileDirectoryPath);
            Profiles.Add(defbind.Name, defbind);
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
                BackstageModel.RegisterEvent(new KeyAssignProfileNotFoundEvent(profileId));
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
                System.Diagnostics.Debug.WriteLine("$ Key assign invoke: " + desc.ActionName);
                try
                {
                    callback.Invoke(desc.Argument);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent(SettingModelResources.KeyAssignError, ex));
                }
                return true;
            }
            System.Diagnostics.Debug.WriteLine("$ Key assign not matched: " + desc.ActionName);
            return false;
        }
    }

    public sealed class KeyAssignAction
    {
        private readonly string _name;

        private readonly bool? _argumentRequired;

        private readonly Action<string> _callback;

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

        public KeyAssignAction(string name, Action<string> callback, bool? argumentRequired = null)
        {
            _name = name;
            _callback = callback;
            this._argumentRequired = argumentRequired;
        }

        public string Name
        {
            get { return _name; }
        }

        public bool? ArgumentRequired
        {
            get { return this._argumentRequired; }
        }

        public void Invoke(string argument)
        {
            if (this._argumentRequired != null && this._argumentRequired.Value == String.IsNullOrEmpty(argument))
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    (this._argumentRequired.Value
                        ? SettingModelResources.KeyAssignErrorArgumentRequiredFormat
                        : SettingModelResources.KeyAssignErrorArgumentUnsupportedFormat)
                        .SafeFormat(Name),
                    null));
            }
            else
            {
                _callback(argument);
            }
        }
    }
}
