using System.Windows.Media;
using Livet;
using StarryEyes.Models.Backstages;

namespace StarryEyes.ViewModels.WindowParts.Backstages
{
    public class BackstageEventViewModel : ViewModel
    {
        private readonly BackstageEventBase _sourceEvent;

        public BackstageEventViewModel(BackstageEventBase ev)
        {
            this._sourceEvent = ev;
        }

        public BackstageEventBase SourceEvent
        {
            get { return this._sourceEvent; }
        }

        public Color Background
        {
            get { return this.SourceEvent.Background; }
        }

        public Color Foreground
        {
            get { return this.SourceEvent.Foreground; }
        }

        public string Title
        {
            get { return this.SourceEvent.Title; }
        }

        public ImageSource TitleImage
        {
            get { return this.SourceEvent.TitleImage; }
        }

        public bool IsImageAvailable
        {
            get { return this.SourceEvent.TitleImage != null; }
        }

        public string Detail
        {
            get { return this.SourceEvent.Detail.Replace("\r", "").Replace("\n", " "); }
        }
    }
}