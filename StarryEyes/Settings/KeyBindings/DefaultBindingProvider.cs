using System;

namespace StarryEyes.Settings.KeyBindings
{
    public static class DefaultBindingProvider
    {
        public static KeyBindingProfile MakeDefault()
        {
            var profile = new KeyBindingProfile("default");
            const string assign = @"[Global]
enter: FocusToInput
c+t: FocusToTimeline
c+q: FocusToSearch
c+a+down: SelectNextAccount
c+a+up: SelectPreviousAccount
c+a+Left: ClearSelectAccount
c+a+Right: SelectAllAccount

[Timeline]
h: SelectLeftTab
j: MoveDown
k: MoveUp
l: SelectRightTab
s+h: SelectLeftColumn
s+j: MoveEnd
s+k: MoveTop
s+l: SelectRightColumn
0: MoveTop

Space: ToggleSelect
f: Favorite
# Semicolon
Oem1: Favorite
s+f: FavoriteMany
r: Reply
c+r: Retweet
c+q: Quote
c+f: Favorite Retweet
c+m: Reply(@{1} もやし)
c+delete: Delete
c+a+delete: Mute

c+t: CreateTab
c+F4: CloseTab
c+s+t: RestoreTab

OemPeriod: ShowConversation

w: OpenWeb
c+w: OpenFavstar
s+1: OpenUrl(0)
s+2: OpenUrl(1)
s+3: OpenUrl(2)

[Input]
escape: CloseInput
#enter: Post
c+enter: Post
c+s: Save
c+space: LoadStash
c+o: AttachImage
c+w: ToggleEscape
c+b: ToggleBind

[Search]
escape: CloseSearch FocusToTimeline
c+t: SearchFromTab
c+l: SearchFromLocal
c+w: SearchFromWeb
c+u: SearchUser
c+s: SearchScreenName
";
            profile.SetBindings(assign.Split(new[] { '\r', '\n' }));
            return profile;
        }
    }
}
