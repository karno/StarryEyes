using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Helpers;
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
                if (status.RetweetedStatus != null)
                {
                    status = status.RetweetedStatus;
                }
                GenerateInlines(o, status.Text, status.Entities)
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

                var desc = user.Description ?? String.Empty;

                // filter and merge entities
                var entities = user.DescriptionEntities == null
                    ? null
                    : user.DescriptionEntities.Where(ent => ent.EntityType == EntityType.Urls)
                          .Concat(GetUserMentionEntities(desc));

                // generate contents
                GenerateInlines(o, desc, entities)
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

        private static IEnumerable<TwitterEntity> GetUserMentionEntities([NotNull] string text)
        {
            // entity indices uses escaped index
            var escaped = ParsingExtension.EscapeEntity(text);
            return TwitterRegexPatterns
                .ValidMentionOrList
                .Matches(escaped)
                .OfType<Match>()
                .Select(m =>
                {
                    var display =
                        m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupAt].Value +
                        m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupUsername].Value;
                    var index = m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupAt].Index;
                    var scIndex = escaped.Substring(0, index).SurrogatedLength();

                    return new TwitterEntity
                    {
                        EntityType = EntityType.UserMentions,
                        DisplayText = display,
                        StartIndex = scIndex,
                        EndIndex = scIndex + display.SurrogatedLength()
                    };
                });
        }

        #endregion

        #region Colors

        public static Brush GetForegroundBrush(DependencyObject obj)
        {
            return (Brush)obj.GetValue(ForegroundBrushProperty);
        }

        public static void SetForegroundBrush(DependencyObject obj, Brush brush)
        {
            obj.SetValue(ForegroundBrushProperty, brush);
        }

        public static readonly DependencyProperty ForegroundBrushProperty =
            DependencyProperty.RegisterAttached(
                "ForegroundBrush",
                typeof(Brush),
                typeof(TextBlockStylizer),
                new PropertyMetadata(Brushes.Black));

        public static Brush GetHyperlinkBrush(DependencyObject obj)
        {
            return (Brush)obj.GetValue(HyperlinkBrushProperty);
        }

        public static void SetHyperlinkBrush(DependencyObject obj, Brush brush)
        {
            obj.SetValue(HyperlinkBrushProperty, brush);
        }

        public static readonly DependencyProperty HyperlinkBrushProperty =
            DependencyProperty.RegisterAttached(
                "HyperlinkBrush",
                typeof(Brush),
                typeof(TextBlockStylizer),
                new PropertyMetadata(Brushes.Black));

        #endregion

        #region Common Entitied Text Utility

        private static IEnumerable<Inline> GenerateInlines([NotNull] DependencyObject obj,
            [NotNull] string text, [CanBeNull] IEnumerable<TwitterEntity> entities)
        {
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
                        case EntityType.Urls:
                            yield return GenerateLink(obj, display, ParsingExtension.ResolveEntity(entity.OriginalUrl));
                            break;
                        case EntityType.UserMentions:
                            yield return
                                GenerateUserLink(obj, display, ParsingExtension.ResolveEntity(entity.DisplayText));
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

        private static IEnumerable<Inline> GenerateInlines([NotNull] DependencyObject obj, [NotNull] string text)
        {
            foreach (var tok in StatusTextUtil.Tokenize(text))
            {
                if (String.IsNullOrEmpty(tok.Text))
                {
                    // skip empty block
                    continue;
                }
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

        private static void SetForeBrushBinding([NotNull] FrameworkContentElement inline, [NotNull] object property)
        {
            var binding = new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TextBlock), 1),
                Path = new PropertyPath(property)
            };
            inline.SetBinding(TextElement.ForegroundProperty, binding);
        }

        private static Inline GenerateText([NotNull] string surface)
        {
            var run = new Run { Text = surface, Focusable = false };
            SetForeBrushBinding(run, ForegroundBrushProperty);
            return run;
        }

        private static Inline GenerateLink([NotNull] DependencyObject obj,
            [NotNull] string surface, [NotNull] string linkUrl)
        {
            var hl = new Hyperlink { Focusable = false };
            hl.Inlines.Add(surface);
            hl.Command = new ProxyCommand(link =>
            {
                var command = GetLinkNavigationCommand(obj);
                if (command != null)
                    command.Execute(link as string);
            });
            hl.CommandParameter = linkUrl;
            SetForeBrushBinding(hl, HyperlinkBrushProperty);
            return hl;
        }

        private static Inline GenerateUserLink([NotNull] DependencyObject obj,
            [NotNull] string surface, [NotNull] string userScreenName)
        {
            if (surface.Length > 0 && surface[0] != '@' && surface[0] != '＠')
            {
                surface = "@" + surface;
            }
            return GenerateLink(obj, surface,
                UserNavigation + userScreenName);
        }

        private static Inline GenerateHashtagLink([NotNull] DependencyObject obj, [NotNull] string surface)
        {
            if (surface.Length > 0 && surface[0] != '#' && surface[0] != '＃')
            {
                surface = "#" + surface;
            }
            return GenerateLink(obj, surface,
                HashtagNavigation + surface);
        }
    }

    public class ProxyCommand : ICommand
    {
        public ProxyCommand([NotNull] Action<object> callback)
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
