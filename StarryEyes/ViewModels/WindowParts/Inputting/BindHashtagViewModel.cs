using System;
using Livet;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class BindHashtagViewModel : ViewModel
    {
        private readonly Action _callback;
        private readonly string _hashtag;

        public BindHashtagViewModel(string hashtag, Action callback)
        {
            this._hashtag = hashtag;
            this._callback = callback;
        }

        public string DisplayHashtag
        {
            get { return "#" + this._hashtag; }
        }

        public string Hashtag
        {
            get { return this._hashtag; }
        }

        public void ToggleBind()
        {
            this._callback();
        }
    }
}