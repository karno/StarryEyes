using System.Collections.ObjectModel;
using Livet;
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
            if (_isChanged)
            {

            }
        }

        public void Save()
        {
            _isChanged = false;
        }

        private void SaveCore(string fileName)
        {

        }
    }
}
