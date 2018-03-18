using System.Windows.Media;
using Livet;
using StarryEyes.Models.Backstages;

namespace StarryEyes.ViewModels.WindowParts.Backstages
{
    public class BackstageEventViewModel : ViewModel
    {
        public BackstageEventViewModel(BackstageEventBase ev)
        {
            SourceEvent = ev;
        }

        protected BackstageEventBase SourceEvent { get; }

        public Color Background => SourceEvent.Background;

        public Color Foreground => SourceEvent.Foreground;

        public string Title => SourceEvent.Title;

        public ImageSource TitleImage => SourceEvent.TitleImage;

        public bool IsImageAvailable => SourceEvent.TitleImage != null;

        public string Detail => SourceEvent.Detail.Replace("\r", "").Replace("\n", " ");
    }
}