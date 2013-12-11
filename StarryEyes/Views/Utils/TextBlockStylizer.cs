using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models;

namespace StarryEyes.Views.Utils
{
    public class TextBlockStylizer
    {
        public const string UserNavigation = "user://";

        public const string HashtagNavigation = "hash://";

        public static Tuple<LinkType, string> ResolveInternalUrl(string iurl)
        {
            if (iurl.StartsWith(UserNavigation))
            {
                return new Tuple<LinkType, string>(LinkType.User, iurl.Substring(UserNavigation.Length));
            }
            if (iurl.StartsWith(HashtagNavigation))
            {
                return new Tuple<LinkType, string>(LinkType.Hash, iurl.Substring(HashtagNavigation.Length));
            }
            return new Tuple<LinkType, string>(LinkType.Url, iurl);
        }

        #region Twitter Status

        public static TwitterStatus GetTwitterStatus(DependencyObject obj)
        {
            return (TwitterStatus)obj.GetValue(TwitterStatusProperty);
        }

        public static void SetTwitterStatus(DependencyObject obj, TwitterStatus value)
        {
            obj.SetValue(TwitterStatusProperty, value);
        }

        public static readonly DependencyProperty TwitterStatusProperty =
            DependencyProperty.RegisterAttached(
            "TwitterStatus",
            typeof(TwitterStatus),
            typeof(TextBlockStylizer),
            new PropertyMetadata((o, e) =>
            {
                var status = (TwitterStatus)e.NewValue;
                var textBlock = (TextBlock)o;

                textBlock.Inlines.Clear();

                if (status == null)
                {
                    return;
                }

                // generate contents
                if (status.RetweetedOriginal != null)
                {
                    status = status.RetweetedOriginal;
                }
                GenerateInlines(o, status.Text, status.Entities, status.StatusType == StatusType.DirectMessage)
                    .ForEach(textBlock.Inlines.Add);
            }));

        #endregion

        #region Twitter Users

        public static TwitterUser GetTwitterUser(DependencyObject obj)
        {
            return (TwitterUser)obj.GetValue(TwitterUserProperty);
        }

        public static void SetTwitterUser(DependencyObject obj, TwitterUser value)
        {
            obj.SetValue(TwitterUserProperty, value);
        }

        public static readonly DependencyProperty TwitterUserProperty =
            DependencyProperty.RegisterAttached(
            "TwitterUser",
            typeof(TwitterUser),
            typeof(TextBlockStylizer),
            new PropertyMetadata((o, e) =>
            {
                var user = (TwitterUser)e.NewValue;
                var textBlock = (TextBlock)o;
                var foreground = textBlock.Foreground;

                textBlock.Inlines.Clear();

                if (user == null)
                {
                    return;
                }

                // generate contents
                GenerateInlines(o, user.Description, user.DescriptionEntities, false)
                    .Select(inline =>
                    {
                        var run = inline as Run;
                        if (run != null)
                        {
                            run.Foreground = foreground;
                        }
                        return inline;
                    })
                    .ForEach(textBlock.Inlines.Add);
            }));

        #endregion

        #region Common Entitied Text Utility

        private static IEnumerable<Inline> GenerateInlines(DependencyObject obj, string text, IEnumerable<TwitterEntity> entities, bool isDirectMessage)
        {
            text = text ?? String.Empty;
            if (entities == null)
            {
                foreach (var inline in GenerateInlines(obj, text))
                {
                    yield return inline;
                }
                yield break;
            }
            foreach (var description in TextEntityResolver.ParseText(text, entities))
            {
                if (!description.IsEntityAvailable)
                {
                    yield return GenerateText(description.Text);
                }
                else
                {
                    var entity = description.Entity;
                    var display = entity.DisplayText;
                    if (String.IsNullOrEmpty(display))
                    {
                        display = entity.OriginalUrl;
                    }
                    switch (entity.EntityType)
                    {
                        case EntityType.Hashtags:
                            yield return GenerateHashtagLink(obj, display);
                            break;
                        case EntityType.Media:
                            if (!isDirectMessage)
                            {
                                yield return GenerateLink(obj, display, ParsingExtension.ResolveEntity(entity.OriginalUrl));
                            }
                            break;
                        case EntityType.Urls:
                            yield return GenerateLink(obj, display, ParsingExtension.ResolveEntity(entity.OriginalUrl));
                            break;
                        case EntityType.UserMentions:
                            yield return GenerateUserLink(obj, display, ParsingExtension.ResolveEntity(entity.DisplayText));
                            break;
                    }
                }
            }
        }

        #endregion

        #region Generic Text

        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(TextBlockStylizer),
            new PropertyMetadata((o, e) =>
            {
                var text = (string)e.NewValue;
                var textBlock = (TextBlock)o;

                textBlock.Inlines.Clear();

                // generate contents
                GenerateInlines(o, text).ForEach(textBlock.Inlines.Add);
            }));

        private static IEnumerable<Inline> GenerateInlines(DependencyObject obj, string text)
        {
            foreach (var tok in StatusTextUtil.Tokenize(text))
            {
                switch (tok.Kind)
                {
                    case TokenKind.Url:
                        yield return GenerateLink(obj, tok.Text, tok.Text);
                        break;
                    case TokenKind.Hashtag:
                        yield return GenerateHashtagLink(obj, tok.Text.Substring(1));
                        break;
                    case TokenKind.AtLink:
                        yield return GenerateUserLink(obj, tok.Text.Substring(1), tok.Text.Substring(1));
                        break;
                    case TokenKind.Text:
                        yield return GenerateText(tok.Text);
                        break;
                }
            }
        }

        #endregion

        #region Link Navigation

        public static ICommand GetLinkNavigationCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(LinkNavigationCommandProperty);
        }

        public static void SetLinkNavigationCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(LinkNavigationCommandProperty, value);
        }

        public static readonly DependencyProperty LinkNavigationCommandProperty =
            DependencyProperty.RegisterAttached(
            "LinkNavigationCommand",
            typeof(ICommand),
            typeof(TextBlockStylizer),
            new PropertyMetadata(null));

        #endregion

        private static Inline GenerateText(string surface)
        {
            return new Run { Text = surface, Focusable = false };
        }

        private static Inline GenerateLink(DependencyObject obj, string surface, string linkUrl)
        {
            var hl = new Hyperlink { Foreground = Brushes.Gray, Focusable = false };
            hl.Inlines.Add(surface);
            hl.Command = new ProxyCommand(link =>
            {
                var command = GetLinkNavigationCommand(obj);
                if (command != null)
                    command.Execute(link as string);
            });
            hl.CommandParameter = linkUrl;
            return hl;
        }

        private static Inline GenerateUserLink(DependencyObject obj, string surface, string userScreenName)
        {
            return GenerateLink(obj,
                surface.StartsWith("@") ? surface : "@" + surface,
                UserNavigation + userScreenName);
        }

        private static Inline GenerateHashtagLink(DependencyObject obj, string surface)
        {
            return GenerateLink(obj,
                surface.StartsWith("#") ? surface : "#" + surface,
                HashtagNavigation + surface);
        }
    }

    public class ProxyCommand : ICommand
    {
        public ProxyCommand(Action<object> callback)
        {
            this._callback = callback;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        private readonly Action<object> _callback;

        public void Execute(object parameter)
        {
            _callback(parameter);
        }
    }

    public enum LinkType
    {
        Url,
        User,
        Hash,
    }
}
