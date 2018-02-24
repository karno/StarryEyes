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
using Cadena.Data;
using Cadena.Data.Entities;
using Cadena.Util;
using JetBrains.Annotations;
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

        #endregion Twitter Status

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
                    var entities = user.DescriptionEntities
                                       .OfType<TwitterUrlEntity>()
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

        private static IEnumerable<TwitterEntity> GetUserMentionEntities([CanBeNull] string text)
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

                    return new TwitterUserMentionEntity(
                        Tuple.Create(scIndex, scIndex + display.SurrogatedLength()),
                        0, display, display
                    );
                });
        }

        #endregion Twitter Users

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

        #endregion Colors

        #region Common Entitied Text Utility

        private static IEnumerable<Inline> GenerateInlines([CanBeNull] DependencyObject obj,
            [CanBeNull] string text, [CanBeNullAttribute] IEnumerable<TwitterEntity> entities)
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
                if (description.Entity == null)
                {
                    yield return GenerateText(description.Text);
                }
                else
                {
                    var entity = description.Entity;
                    if (entity is TwitterHashtagEntity he)
                    {
                        yield return GenerateHashtagLink(obj, he.DisplayText);
                    }
                    else if (entity is TwitterMediaEntity me)
                    {
                        yield return GenerateLink(obj, me.DisplayText,
                            ParsingExtension.ResolveEntity(me.MediaUrlHttps));
                    }
                    else if (entity is TwitterUrlEntity ue)
                    {
                        yield return GenerateLink(obj, ue.DisplayText,
                            ParsingExtension.ResolveEntity(ue.ExpandedUrl));
                    }
                    else if (entity is TwitterUserMentionEntity re)
                    {
                        yield return GenerateUserLink(obj, re.DisplayText,
                            ParsingExtension.ResolveEntity(re.DisplayText));
                    }
                }
            }
        }

        #endregion Common Entitied Text Utility

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

        private static IEnumerable<Inline> GenerateInlines([CanBeNull] DependencyObject obj, [CanBeNull] string text)
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

        #endregion Generic Text

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

        #endregion Link Navigation

        private static void SetForeBrushBinding([CanBeNull] FrameworkContentElement inline, [CanBeNull] object property)
        {
            var binding = new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TextBlock), 1),
                Path = new PropertyPath(property)
            };
            inline.SetBinding(TextElement.ForegroundProperty, binding);
        }

        private static Inline GenerateText([CanBeNull] string surface)
        {
            var run = new Run { Text = surface, Focusable = false };
            SetForeBrushBinding(run, ForegroundBrushProperty);
            return run;
        }

        private static Inline GenerateLink([CanBeNull] DependencyObject obj,
            [CanBeNull] string surface, [CanBeNull] string linkUrl)
        {
            var hl = new Hyperlink { Focusable = false };
            hl.Inlines.Add(surface);
            hl.Command = new ProxyCommand(link =>
            {
                var command = GetLinkNavigationCommand(obj);
                command?.Execute(link as string);
            });
            hl.CommandParameter = linkUrl;
            SetForeBrushBinding(hl, HyperlinkBrushProperty);
            return hl;
        }

        private static Inline GenerateUserLink([CanBeNull] DependencyObject obj,
            [CanBeNull] string surface, [CanBeNull] string userScreenName)
        {
            if (surface.Length > 0 && surface[0] != '@' && surface[0] != '＠')
            {
                surface = "@" + surface;
            }
            return GenerateLink(obj, surface,
                UserNavigation + userScreenName);
        }

        private static Inline GenerateHashtagLink([CanBeNull] DependencyObject obj, [CanBeNull] string surface)
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
        public ProxyCommand([CanBeNull] Action<object> callback)
        {
            _callback = callback;
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