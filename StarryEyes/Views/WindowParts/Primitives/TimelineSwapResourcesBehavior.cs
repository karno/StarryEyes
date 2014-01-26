using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace StarryEyes.Views.WindowParts.Primitives
{
    public class TimelineSwapResourcesBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty ResourceSetProperty = DependencyProperty.RegisterAttached(
            "ResourceSet", typeof(TimelineResourceSet), typeof(TimelineSwapResourcesBehavior),
            new PropertyMetadata(TimelineResourceSet.Invalid, ResourceSetChanged));

        private static void ResourceSetChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var fe = o as FrameworkElement;
            if (fe == null || e.OldValue == e.NewValue) return;
            fe.Resources.MergedDictionaries.Add(TimelineResourceSets[(int)e.NewValue]);
            if (e.OldValue != null && (int)e.OldValue >= 0)
            {
                fe.Resources.MergedDictionaries.Remove(TimelineResourceSets[(int)e.OldValue]);
            }
        }

        public static void SetResourceSet(DependencyObject element, TimelineResourceSet value)
        {
            element.SetValue(ResourceSetProperty, value);
        }

        private static readonly DependencyProperty ResourceSourceNameProperty = DependencyProperty.Register(
            "ResourceSourceName", typeof(string), typeof(TimelineSwapResourcesBehavior),
            new PropertyMetadata(null));

        public static TimelineResourceSet GetResourceSet(DependencyObject element)
        {
            return (TimelineResourceSet)element.GetValue(ResourceSetProperty);
        }

        private static readonly ResourceDictionary[] TimelineResourceSets = CreateResources().ToArray();

        private static IEnumerable<ResourceDictionary> CreateResources()
        {
            var keys = new[]
            {
                "TweetBackgroundBrush",
                "TweetForegroundBrush",
                "TweetHighlightBackgroundBrush",
                "TweetHighlightForegroundBrush",
                "KeyTextBrush",
                "SubTextBrush",
                "HyperlinkTextBrush",
                "FavoriteCounter",
                "RetweetCounter",
                "RetweetMarker",
                "FavAndRtBackground",
                "FavAndRtForeground",
                "FavAndRtHoveringBackground",
                "FavAndRtHoveringForeground",
                "FavAndRtPressedBackground",
                "FavAndRtPressedForeground",
                "FavoriteBackground",
                "FavoriteForeground",
                "FavoriteHoveringBackground",
                "FavoriteHoveringForeground",
                "FavoritePressedBackground",
                "FavoritePressedForeground",
                "ColoredFavoriteBackground",
                "ColoredFavoriteForeground",
                "ColoredFavoriteHoveringBackground",
                "ColoredFavoriteHoveringForeground",
                "ColoredFavoritePressedBackground",
                "ColoredFavoritePressedForeground",
                "RetweetBackground",
                "RetweetForeground",
                "RetweetHoveringBackground",
                "RetweetHoveringForeground",
                "RetweetPressedBackground",
                "RetweetPressedForeground",
                "ColoredRetweetBackground",
                "ColoredRetweetForeground",
                "ColoredRetweetHoveringBackground",
                "ColoredRetweetHoveringForeground",
                "ColoredRetweetPressedBackground",
                "ColoredRetweetPressedForeground",
                "MentionBackground",
                "MentionForeground",
                "MentionHoveringBackground",
                "MentionHoveringForeground",
                "MentionPressedBackground",
                "MentionPressedForeground",
                "DeleteBackground",
                "DeleteForeground",
                "DeleteHoveringBackground",
                "DeleteHoveringForeground",
                "DeletePressedBackground",
                "DeletePressedForeground"
            };
            var resourceChannels = new[]
            {
                "Default",
                "Myself",
                "Mention",
                "Retweet",
                "Message"
            };
            var resources = new[]
            {
                "Tweet{0}Background",
                "Tweet{0}Foreground",
                "Tweet{0}HighlightBackground",
                "Tweet{0}HighlightForeground",
                "Tweet{0}KeyText",
                "Tweet{0}SubText",
                "Tweet{0}HyperlinkText",
                "Tweet{0}FavoriteCounter",
                "Tweet{0}FavoriteCounter",
                "Tweet{0}RetweetMarker",
                "Tweet{0}FavAndRtButtonBackground",
                "Tweet{0}FavAndRtButtonForeground",
                "Tweet{0}FavAndRtButtonHoveringBackground",
                "Tweet{0}FavAndRtButtonHoveringForeground",
                "Tweet{0}FavAndRtButtonPressedBackground",
                "Tweet{0}FavAndRtButtonPressedForeground",
                "Tweet{0}FavoriteButtonBackground",
                "Tweet{0}FavoriteButtonForeground",
                "Tweet{0}FavoriteButtonHoveringBackground",
                "Tweet{0}FavoriteButtonHoveringForeground",
                "Tweet{0}FavoriteButtonPressedBackground",
                "Tweet{0}FavoriteButtonPressedForeground",
                "Tweet{0}ColoredFavoriteButtonBackground",
                "Tweet{0}ColoredFavoriteButtonForeground",
                "Tweet{0}ColoredFavoriteButtonHoveringBackground",
                "Tweet{0}ColoredFavoriteButtonHoveringForeground",
                "Tweet{0}ColoredFavoriteButtonPressedBackground",
                "Tweet{0}ColoredFavoriteButtonPressedForeground",
                "Tweet{0}RetweetButtonBackground",
                "Tweet{0}RetweetButtonForeground",
                "Tweet{0}RetweetButtonHoveringBackground",
                "Tweet{0}RetweetButtonHoveringForeground",
                "Tweet{0}RetweetButtonPressedBackground",
                "Tweet{0}RetweetButtonPressedForeground",
                "Tweet{0}ColoredRetweetButtonBackground",
                "Tweet{0}ColoredRetweetButtonForeground",
                "Tweet{0}ColoredRetweetButtonHoveringBackground",
                "Tweet{0}ColoredRetweetButtonHoveringForeground",
                "Tweet{0}ColoredRetweetButtonPressedBackground",
                "Tweet{0}ColoredRetweetButtonPressedForeground",
                "Tweet{0}MentionButtonBackground",
                "Tweet{0}MentionButtonForeground",
                "Tweet{0}MentionButtonHoveringBackground",
                "Tweet{0}MentionButtonHoveringForeground",
                "Tweet{0}MentionButtonPressedBackground",
                "Tweet{0}MentionButtonPressedForeground",
                "Tweet{0}DeleteButtonBackground",
                "Tweet{0}DeleteButtonForeground",
                "Tweet{0}DeleteButtonHoveringBackground",
                "Tweet{0}DeleteButtonHoveringForeground",
                "Tweet{0}DeleteButtonPressedBackground",
                "Tweet{0}DeleteButtonPressedForeground"
            };
            for (int i = 0; i < resourceChannels.Length; i++)
            {
                var nrd = new ResourceDictionary();
                var cmod = resourceChannels[i];
                resources
                    .Select(s => String.Format(s, cmod))
                    .Zip(keys, (r, k) =>
                    {
                        var sb = new SolidColorBrush();
                        sb.SetValue(ResourceSourceNameProperty, r);
                        sb.SetValue(SolidColorBrush.ColorProperty, Application.Current.FindResource(r));
                        return new { Key = k, Resource = sb };
                    })
                    .ForEach(b => nrd.Add(b.Key, b.Resource));
                yield return nrd;
            }
        }

        public static void RefreshResources()
        {
            foreach (var dictionary in TimelineResourceSets)
            {
                var list = new List<KeyValuePair<object, SolidColorBrush>>();
                foreach (var key in dictionary.Keys)
                {
                    var sb = dictionary[key] as SolidColorBrush;
                    if (sb == null) continue;
                    var r = (string)sb.GetValue(ResourceSourceNameProperty);
                    var nsb = new SolidColorBrush();
                    nsb.SetValue(ResourceSourceNameProperty, r);
                    nsb.SetValue(SolidColorBrush.ColorProperty, Application.Current.FindResource(r));
                    list.Add(new KeyValuePair<object, SolidColorBrush>(key, nsb));
                }
                list.ForEach(p => dictionary[p.Key] = p.Value);
            }
        }
    }

    public enum TimelineResourceSet
    {
        Default,
        Myself,
        Mention,
        Retweet,
        Message,
        Invalid = -1
    }
}
