using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.Pkcs;
using Livet;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Views.Messaging;
using ThemeManager = StarryEyes.Settings.ThemeManager;

namespace StarryEyes.ViewModels.WindowParts.Flips.SettingFlips
{
    public class ThemeEditorViewModel : ViewModel
    {
        public ThemeEditorViewModel()
        {
            RefreshCandidates();
        }

        private readonly ObservableCollection<string> _themeCandidateFiles = new ObservableCollection<string>();
        public ObservableCollection<string> ThemeCandidateFiles
        {
            get { return this._themeCandidateFiles; }
        }

        public void RefreshThemes()
        {
            ThemeManager.ReloadCandidates();
            RefreshCandidates();
        }

        private void RefreshCandidates()
        {
        }

        private bool _isChanged = false;

        private void ConfirmOnUnsaved()
        {

        }

        public void Save()
        {

        }

        private void SaveCore(string fileName)
        {

        }
    }
}
