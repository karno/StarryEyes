using System.Collections.ObjectModel;
using Livet;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Flips.SettingFlips
{
    public class ThemeEditorViewModel : ViewModel
    {
        public ThemeEditorViewModel()
        {
            RefreshCandidates();
        }

        public ObservableCollection<string> ThemeCandidateFiles { get; } = new ObservableCollection<string>();

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