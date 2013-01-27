using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using StarryEyes.Breezy.Util;
using System.Windows.Input;
using StarryEyes.Breezy.DataModel;
using System.Windows.Controls;
using StarryEyes.Models;
using System.Windows.Media;
using Livet.Commands;

namespace StarryEyes.Views.Helpers
{
    public class StatusStylizeHelper
    {
        public static string UserNavigation = "user://";

        public static string HashtagNavigation = "hash://";

        public static Tuple<LinkType, string> ResolveInternalUrl(string iurl)
        {
            if (iurl.StartsWith(UserNavigation))
            {
                return new Tuple<LinkType, string>(LinkType.User, iurl.Substring(UserNavigation.Length));
            }
            else if (iurl.StartsWith(HashtagNavigation))
            {
                return new Tuple<LinkType, string>(LinkType.Hash, iurl.Substring(HashtagNavigation.Length));
            }
            else
            {
                return new Tuple<LinkType, string>(LinkType.Url, iurl);
            }
        }

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
            typeof(StatusStylizeHelper),
            new PropertyMetadata((o, e) =>
            {
                var status = (TwitterStatus)e.NewValue;
                var text = (TextBlock)o;

                text.Inlines.Clear();

                if (status == null)
                    return;

                // generate contents
                if (status.IsDataLacking)
                {
                    GenerateInlines(o, (status.Text)).ForEach(text.Inlines.Add);
                }
                else
                {
                    GenerateInlines(o, status).ForEach(text.Inlines.Add);
                }
            }));

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
            typeof(StatusStylizeHelper),
            new PropertyMetadata((o, e) =>
            {
                var text = (string)e.NewValue;
                var richText = (RichTextBox)o;
                var paragraph = new Paragraph();
                // generate contents
                GenerateInlines(o, text).ForEach(i => paragraph.Inlines.Add(i));
                richText.Document.Blocks.Clear();
                richText.Document.IsEnabled = true;
                richText.Document.Blocks.Add(paragraph);
            }));

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
            typeof(StatusStylizeHelper),
            new PropertyMetadata(null));

        private static IEnumerable<Inline> GenerateInlines(DependencyObject obj, TwitterStatus status)
        {
            if (status.RetweetedOriginal != null)
                status = status.RetweetedOriginal; // change target
            string text = status.Text;
            TwitterEntity prevEntity = null;
            foreach (var entity in status.Entities.Guard().OrderBy(e => e.StartIndex))
            {
                int pidx = 0;
                if (prevEntity != null)
                    pidx = prevEntity.EndIndex;
                if (pidx < entity.StartIndex)
                {
                    // output raw
                    yield return GenerateText(
                        status.Text.Substring(pidx, entity.StartIndex - pidx));
                }
                switch (entity.EntityType)
                {
                    case EntityType.Hashtags:
                        yield return GenerateHashtagLink(obj, entity.DisplayText);
                        break;
                    case EntityType.Media:
                    case EntityType.Urls:
                        yield return GenerateLink(obj, entity.DisplayText, entity.OriginalText);
                        break;
                    case EntityType.UserMentions:
                        yield return GenerateUserLink(obj, entity.DisplayText, entity.DisplayText);
                        break;
                }
                prevEntity = entity;
            }
            if (prevEntity == null)
            {
                yield return GenerateText(XmlParser.ResolveEntity(status.Text));
            }
            else if (prevEntity.EndIndex < status.Text.Length)
            {
                yield return GenerateText(
                    status.Text.Substring(prevEntity.EndIndex, status.Text.Length - prevEntity.EndIndex));
            }
        }

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

        private static Inline GenerateText(string surface)
        {
            var run = new Run();
            run.Text = XmlParser.ResolveEntity(surface);
            return run;
        }

        private static Inline GenerateLink(DependencyObject obj, string surface, string linkUrl)
        {
            var hl = new Hyperlink { Foreground = Brushes.Gray };
            hl.Inlines.Add(XmlParser.ResolveEntity(surface));
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
            return GenerateLink(obj, "@" + surface, UserNavigation + userScreenName);
        }

        private static Inline GenerateHashtagLink(DependencyObject obj, string surface)
        {
            return GenerateLink(obj, "#" + surface, HashtagNavigation + surface);
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

        public event EventHandler CanExecuteChanged;
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
