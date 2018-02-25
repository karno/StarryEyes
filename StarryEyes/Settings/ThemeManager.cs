using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using StarryEyes.Globalization.Models;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings.Themes;

namespace StarryEyes.Settings
{
    public static class ThemeManager
    {
        private static readonly IDictionary<string, ThemeProfile> ThemeProfiles =
            new Dictionary<string, ThemeProfile>();

        public static IEnumerable<string> Themes => ThemeProfiles.Keys;

        public static string ThemeProfileDirectoryPath =>
            Path.Combine(App.ConfigurationDirectoryPath, App.ThemeProfilesDirectory);

        public static event Action ThemeChanged;

        internal static void Initialize()
        {
            Directory.CreateDirectory(ThemeProfileDirectoryPath);

            ReloadCandidates();

            Setting.Theme.ValueChanged += _ => ThemeChanged?.Invoke();
        }

        private static void Load(string file)
        {
            if (!File.Exists(file)) return;
            try
            {
                var profile = ThemeProfile.Load(file);
                if (profile.ProfileVersion < ThemeProfile.CurrentProfileVersion)
                {
                    MainWindowModel.ShowTaskDialog(
                        new TaskDialogOptions
                        {
                            Title = SettingModelResources.ThemeIncompatibleTitle,
                            MainIcon = VistaTaskDialogIcon.Warning,
                            MainInstruction = SettingModelResources.ThemeIncompatibleInst,
                            Content = SettingModelResources.ThemeIncompatibleContent + Environment.NewLine + file,
                            ExpandedInfo = SettingModelResources.ThemeIncompatibleExInfo,
                            CommonButtons = TaskDialogCommonButtons.Close
                        });
                    return;
                }
                ThemeProfiles[profile.Name] = profile;
            }
            catch (Exception ex)
            {
                MainWindowModel.ShowTaskDialog(
                    new TaskDialogOptions
                    {
                        Title = SettingModelResources.ThemeErrorTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = SettingModelResources.ThemeErrorInst,
                        Content = SettingModelResources.ThemeErrorContent + Environment.NewLine + file,
                        ExpandedInfo = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close
                    });
            }
        }

        private static void CheckSetting()
        {
            // prepare default themes
            var defaults = new[] { BuiltInThemeProvider.GetDefault(), BuiltInThemeProvider.GetDarkDefault() };
            foreach (var profile in defaults)
            {
                if (!ThemeProfiles.ContainsKey(profile.Name))
                {
                    profile.Save(ThemeProfileDirectoryPath);
                    ThemeProfiles.Add(profile.Name, profile);
                }
            }

            // check assign is existed
            var group = Setting.Theme.Value;
            if (group == null || !ThemeProfiles.ContainsKey(group))
            {
                // set as default
                Setting.Theme.Value = BuiltInThemeProvider.DefaultThemeName;
            }
        }

        public static void ReloadCandidates()
        {
            var path = ThemeProfileDirectoryPath;

            // clean-up previous
            ThemeProfiles.Clear();

            // load all assigns.
            foreach (var file in Directory.EnumerateFiles(path, "*.xml"))
            {
                Load(file);
            }
            CheckSetting();
        }

        public static ThemeProfile CurrentTheme
        {
            get
            {
                var profileId = Setting.Theme.Value ?? BuiltInThemeProvider.DefaultThemeName;
                if (ThemeProfiles.ContainsKey(profileId))
                {
                    return ThemeProfiles[profileId];
                }

                // not found
                BackstageModel.RegisterEvent(new ThemeProfileNotFoundEvent(profileId));
                return BuiltInThemeProvider.GetDefault();
            }
        }

        [CanBeNull]
        public static ThemeProfile GetTheme(string themeName)
        {
            ThemeProfile result;
            return ThemeProfiles.TryGetValue(themeName, out result) ? result : null;
        }
    }
}