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
            _hashtag = hashtag;
            _callback = callback;
        }

        public string DisplayHashtag => "#" + _hashtag;

        public string Hashtag => _hashtag;

        public void ToggleBind()
        {
            _callback();
        }
    }
}