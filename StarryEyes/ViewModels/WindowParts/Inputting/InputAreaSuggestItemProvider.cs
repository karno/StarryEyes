using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Stores;
using StarryEyes.Views.Controls;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class InputAreaSuggestItemProvider : SuggestItemProviderBase
    {
        public override int CandidateSelectionIndex { get; set; }
        public override string FindNearestToken(string text, int caretIndex, out int tokenStart, out int tokenLength)
        {
            tokenStart = caretIndex - 1;
            tokenLength = 1;
            while (tokenStart >= 0)
            {
                if (this.CheckTriggerCharInputted(text[tokenStart]))
                {
                    return text.Substring(tokenStart, tokenLength);
                }
                tokenStart--;
                tokenLength++;
            }
            return null;
        }

        public override void UpdateCandidateList(string token, int offset)
        {
            if (String.IsNullOrEmpty(token) || (token[0] != '@' && token[0] != '#'))
            {
                this._items.Clear();
            }
            else
            {
                if (token[0] == '@')
                {
                    var sn = token.Substring(1);
                    if (token.Length == 1)
                    {
                        // pre-clear
                        this._items.Clear();
                    }
                    Task.Run(() => this.AddUserItems(sn));
                }
                else
                {
                    this._items.Clear();
                    this.AddHashItems(token.Substring(1));
                    this.SelectSuitableItem(token);
                }
            }
        }

        private void SelectSuitableItem(string token)
        {
            var array = this._items.Select(s => s.Body.Substring(1))
                              .ToArray();
            while (token.Length > 1)
            {
                var find = token.Substring(1);
                var idx = array.Select((v, i) => new { Item = v, Index = i })
                               .Where(t => t.Item.StartsWith(find, StringComparison.CurrentCultureIgnoreCase))
                               .Select(t => t.Index)
                               .Append(-1)
                               .First();
                if (idx >= 0)
                {
                    this.CandidateSelectionIndex = idx;
                    this.RaisePropertyChanged("CandidateSelectionIndex");
                    break;
                }
                token = token.Substring(0, token.Length - 1);
            }
        }

        public override bool CheckTriggerCharInputted(char inputchar)
        {
            switch (inputchar)
            {
                case '@':
                case '#':
                    return true;
            }
            return false;
        }

        private async Task AddUserItems(string key)
        {
            Debug.WriteLine("current screen name: " + key);
            if (String.IsNullOrEmpty(key))
            {
                await DispatcherHelper.UIDispatcher.InvokeAsync(() => this._items.Clear());
                return;
            }
            var items = (await UserProxy.GetUsersFastAsync(key, 1000))
                .Select(t => t.Item2)
                .ToArray();
            // re-ordering
            var ordered = items.Where(s => s.StartsWith(key))
                               .OrderBy(s => s)
                               .Concat(items.Where(s => !s.StartsWith(key))
                                            .OrderBy(s => s))
                               .Select(s => new SuggestItemViewModel("@" + s))
                               .ToArray();
            await DispatcherHelper.UIDispatcher.InvokeAsync(
                () =>
                {
                    this._items.Clear();
                    ordered.ForEach(s => this._items.Add(s));
                    this.SelectSuitableItem("@" + key);
                });
        }

        private void AddHashItems(string key)
        {
            CacheStore.HashtagCache
                      .Where(s => String.IsNullOrEmpty(key) ||
                                  s.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                      .OrderBy(_ => _)
                      .Select(s => new SuggestItemViewModel("#" + s))
                      .ForEach(s => this._items.Add(s));
        }

        private readonly ObservableCollection<SuggestItemViewModel> _items = new ObservableCollection<SuggestItemViewModel>();
        public override IList CandidateCollection
        {
            get { return this._items; }
        }
    }

    public class SuggestItemViewModel : ViewModel
    {
        public SuggestItemViewModel(string body)
        {
            this.Body = body;
        }

        public string Body { get; set; }

        public override string ToString()
        {
            return Body;
        }
    }
}