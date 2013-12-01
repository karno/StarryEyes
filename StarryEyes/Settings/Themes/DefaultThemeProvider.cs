
using System;
using System.Windows.Media;
using StarryEyes.Views;

namespace StarryEyes.Settings.Themes
{
    public static class DefaultThemeProvider
    {
        public const string DefaultThemeName = "default";

        public static ThemeProfile GetEmpty(string name)
        {
            return new ThemeProfile(name);
        }

        public static ThemeProfile GetDefault()
        {
            var profile = new ThemeProfile(DefaultThemeName);

            // global font is default

            #region base color setting

            profile.GlobalColor = new HighlightableColorTheme
            {
                Default = new ColorTheme
                {
                    Background = Colors.White,
                    Foreground = Colors.Black
                },
                Highlight = new ColorTheme
                {
                    Background = MetroColors.Cyan,
                    Foreground = Colors.White
                }
            };

            profile.BackstageColor = new HighlightableColorTheme
            {
                Default = new ColorTheme
                {
                    Background = Colors.WhiteSmoke,
                    Foreground = Colors.Black
                },
                Highlight = new ColorTheme
                {
                    Background = Colors.WhiteSmoke,
                    Foreground = MetroColors.Cyan
                }
            };

            profile.AccountSelectionFlipColor = new ColorTheme
            {
                Background = Color.FromRgb(0x11, 0x11, 0x11),
                Foreground = Colors.White
            };

            profile.SearchFlipColor = new HighlightableColorTheme
            {
                Default = new ColorTheme
                {
                    Background = Color.FromRgb(0x16, 0x49, 0x9a),
                    Foreground = Colors.White
                },
                Highlight = new ColorTheme
                {
                    Background = Color.FromRgb(0x5c, 0x83, 0xc2),
                    Foreground = Colors.White
                }
            };

            profile.UserFlipColor = new UserFlipTheme
            {
                Default = new ColorTheme
                {
                    Background = Color.FromRgb(0x11, 0x11, 0x11),
                    Foreground = Colors.White
                },
                Highlight = new ColorTheme
                {
                    Background = Color.FromRgb(0x33, 0x33, 0x33),
                    Foreground = Colors.White
                },
                Key = MetroColors.Cyan,
                NotFollowed = Color.FromRgb(0x33, 0x33, 0x33),
                NotFollowbacked = Color.FromRgb(0x80, 0x80, 0x80),
                Following = MetroColors.Cyan,
                Followbacked = MetroColors.Cyan,
                Blocking = MetroColors.Red
            };

            profile.Tab = new TabPreference
            {
                TabFont = new ThemeFont() { FontSize = 16.0 },
                Default = Color.FromRgb(0x99, 0x99, 0x99),
                Selected = MetroColors.Cyan,
                Focused = Colors.Black,
                UnreadCount = MetroColors.Cyan
            };

            #endregion

            #region status color setting

            var alpha = (Func<Color, byte, Color>)((c, a) =>
            {
                c.A = a;
                return c;
            });

            profile.TweetDefault = CreateDefault(MetroColors.Cyan, Colors.Transparent,
                                                 alpha(Colors.Black, 0x10));

            profile.TweetMyself = CreateDefault(MetroColors.Cyan,
                                                alpha(MetroColors.Cyan, 0x16),
                                                alpha(MetroColors.Cyan, 0x20));

            profile.TweetMention = CreateDefault(MetroColors.Orange,
                                                alpha(MetroColors.Orange, 0x16),
                                                alpha(MetroColors.Orange, 0x20));

            profile.TweetRetweet = CreateDefault(MetroColors.Emerald,
                                                alpha(MetroColors.Emerald, 0x16),
                                                alpha(MetroColors.Emerald, 0x20));

            profile.TweetDirectMessage = CreateDefault(MetroColors.Crimson,
                                                alpha(MetroColors.Crimson, 0x16),
                                                alpha(MetroColors.Crimson, 0x20));

            #endregion

            return profile;
        }

        private static TweetTheme CreateDefault(Color key, Color background, Color highlight)
        {
            var dc = new HighlightableColorTheme
            {
                Default = new ColorTheme
                {
                    Background = Colors.Transparent,
                    Foreground = Color.FromRgb(0x80, 0x80, 0x80),
                },
                Highlight = new ColorTheme
                {
                    Background = Color.FromRgb(0xdc, 0xdc, 0xdc),
                    Foreground = Color.FromRgb(0x80, 0x80, 0x80),
                }
            };
            var dcf = (Func<Color, HighlightableColorTheme>)(c => new HighlightableColorTheme
            {
                Default = new ColorTheme
                {
                    Background = Colors.Transparent,
                    Foreground = c,
                },
                Highlight = new ColorTheme
                {
                    Background = Color.FromRgb(0xdc, 0xdc, 0xdc),
                    Foreground = c,
                }
            });
            return new TweetTheme
            {
                Colors = new HighlightableColorTheme
                {
                    Default = new ColorTheme
                    {
                        Background = background,
                        Foreground = Colors.Black,
                    },
                    Highlight = new ColorTheme
                    {
                        Background = highlight,
                        Foreground = Colors.Black,
                    }
                },
                ForegroundKey = key,
                ForegroundSub = Color.FromRgb(0x80, 0x80, 0x80),
                FavoriteAndRetweetButton = dc,
                FavoriteButton = dc,
                ColoredFavoriteButton = dcf(MetroColors.Amber),
                ColoredRetweetButton = dc,
                RetweetButton = dcf(MetroColors.Emerald),
                MentionButton = dc
            };
        }
    }
}
