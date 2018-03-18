using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;
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
                if (CheckTriggerCharInputted(text[tokenStart]))
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
                _items.Clear();
            }
            else
            {
                if (token[0] == '@')
                {
                    var sn = token.Substring(1);
                    if (token.Length == 1)
                    {
                        // pre-clear
                        _items.Clear();
                    }
                    Task.Run(() => AddUserItems(sn));
                }
                else
                {
                    _items.Clear();
                    AddHashItems(token.Substring(1));
                    SelectSuitableItem(token);
                }
            }
        }

        private void SelectSuitableItem(string token)
        {
            var array = _items.Select(s => s.Body.Substring(1))
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
                    CandidateSelectionIndex = idx;
                    RaisePropertyChanged(nameof(CandidateSelectionIndex));
                    break;
                }
                token = token.Substring(0, token.Length - 1);
            }
        }

        public override bool CheckTriggerCharInputted(char inputchar)
        {
            if (!Setting.IsInputSuggestEnabled.Value)
            {
                return false;
            }
            switch (inputchar)
            {
                case '@':
                case '#':
                    return true;
                default:
                    return false;
            }
        }

        private async Task AddUserItems(string key)
        {
            Debug.WriteLine("current screen name: " + key);
            if (String.IsNullOrEmpty(key))
            {
                await DispatcherHelper.UIDispatcher.InvokeAsync(() => _items.Clear());
                return;
            }
            string[] items;
            if (Setting.InputUserSuggestMode.Value == InputUserSuggestMode.All)
            {
                items = (await UserProxy.GetUsersFastAsync(key, 1000))
                    .Select(t => t.Item2)
                    .ToArray();
            }
            else
            {
                items = (await UserProxy.GetRelatedUsersFastAsync(key,
                        Setting.InputUserSuggestMode.Value == InputUserSuggestMode.FollowingsOnly, 1000))
                    .Select(t => t.Item2)
                    .ToArray();
            }
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
                    _items.Clear();
                    ordered.ForEach(s => _items.Add(s));
                    SelectSuitableItem("@" + key);
                });
        }

        private void AddHashItems(string key)
        {
            CacheStore.HashtagCache
                      .Where(s => String.IsNullOrEmpty(key) ||
                                  s.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                      .OrderBy(_ => _)
                      .Select(s => new SuggestItemViewModel("#" + s))
                      .ForEach(s => _items.Add(s));
        }

        private readonly ObservableCollection<SuggestItemViewModel> _items =
            new ObservableCollection<SuggestItemViewModel>();

        public override IList CandidateCollection => _items;
    }

    public class SuggestItemViewModel : ViewModel
    {
        public SuggestItemViewModel(string body)
        {
            Body = body;
        }

        public string Body { get; }

        public override string ToString()
        {
            return Body;
        }
    }
}