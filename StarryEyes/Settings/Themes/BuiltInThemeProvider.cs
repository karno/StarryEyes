using System;
using System.Linq;
using System.Windows.Media;
using StarryEyes.Views;

namespace StarryEyes.Settings.Themes
{
    public static class BuiltInThemeProvider
    {
        public static ThemeProfile GetEmpty(string name)
        {
            return new ThemeProfile(name);
        }

        public const string DefaultThemeName = "default";

        public static ThemeProfile GetDefault()
        {
            var profile = new ThemeProfile(DefaultThemeName);
            profile.ProfileVersion = ThemeProfile.CurrentProfileVersion;

            // global font is default

            #region base color setting

            profile.GlobalFont = FontTheme.Default;

            profile.BaseColor = new ThemeColors
            {
                Background = Colors.White,
                Foreground = Colors.Black
            };

            profile.GlobalKeyColor = MetroColors.Cyan;

            profile.TitleBarColor = new ThemeColors
            {
                Background = MetroColors.Cyan,
                Foreground = Colors.White,
            };

            profile.BackstageColor = new ThemeColors
            {
                Background = Colors.WhiteSmoke,
                Foreground = Colors.Black
            };

            profile.AccountSelectionFlipColor = new ThemeColors
            {
                Background = Color.FromRgb(0x11, 0x11, 0x11),
                Foreground = Colors.White
            };

            profile.SearchFlipColor = new SearchFlipTheme
            {
                Default = new ThemeColors
                {
                    Background = Color.FromRgb(0x1b, 0x58, 0xb8),
                    Foreground = Colors.White
                },
                Pressed = new ThemeColors
                {
                    Background = Color.FromRgb(0x16, 0x49, 0x9a),
                    Foreground = Colors.White
                },
                Selected = new ThemeColors
                {
                    Background = Color.FromRgb(0x5c, 0x83, 0xc2),
                    Foreground = Colors.White
                },
                Hovering = new ThemeColors
                {
                    Background = Color.FromRgb(0x5c, 0x83, 0xc2),
                    Foreground = Colors.White
                },
                QueryInvalid = new ThemeColors
                {
                    Background = MetroColors.Crimson,
                    Foreground = Colors.White
                }
            };

            profile.UserFlipColor = new UserFlipTheme
            {
                Default = new ThemeColors
                {
                    Background = Color.FromRgb(0x11, 0x11, 0x11),
                    Foreground = Colors.White
                },
                Selected = new ThemeColors
                {
                    Background = Color.FromRgb(0x33, 0x33, 0x33),
                    Foreground = Colors.White
                },
                Hovering = new ThemeColors
                {
                    Background = Color.FromRgb(0x22, 0x22, 0x22),
                    Foreground = Colors.White
                },
                Pressed = new ThemeColors
                {

                    Background = Color.FromRgb(0x44, 0x44, 0x44),
                    Foreground = Colors.White
                },
                NotFollowbacked = Colors.DimGray,
                Followbacked = MetroColors.Cyan,
                NotFollowed = Color.FromRgb(0x33, 0x33, 0x33),
                Following = MetroColors.Cyan,
                Blocking = MetroColors.Red
            };

            profile.TabColor = new TabTheme
            {
                TabFont = FontTheme.CreateDefaultWithSize(16.0),
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

            profile.TweetDefaultColor = CreateDefault(MetroColors.Cyan,
                Colors.Black,
                Colors.Transparent,
                alpha(Colors.Black, 0x10));

            profile.TweetMyselfColor = CreateDefault(MetroColors.Cyan,
                Colors.Black,
                alpha(MetroColors.Cyan, 0x16),
                alpha(MetroColors.Cyan, 0x20));

            profile.TweetMentionColor = CreateDefault(MetroColors.Orange,
                Colors.Black,
                alpha(MetroColors.Orange, 0x16),
                alpha(MetroColors.Orange, 0x20));

            profile.TweetRetweetColor = CreateDefault(MetroColors.Emerald,
                Colors.Black,
                alpha(MetroColors.Emerald, 0x16),
                alpha(MetroColors.Emerald, 0x20));

            profile.TweetDirectMessageColor = CreateDefault(MetroColors.Crimson,
                Colors.Black,
                alpha(MetroColors.Crimson, 0x16),
                alpha(MetroColors.Crimson, 0x20));

            #endregion

            return profile;
        }

        public const string DarkThemeName = "starry night";

        public static ThemeProfile GetDarkDefault()
        {
            var profile = new ThemeProfile(DarkThemeName);
            profile.ProfileVersion = ThemeProfile.CurrentProfileVersion;

            // global font is default

            #region base color setting

            profile.GlobalFont = FontTheme.Default;

            profile.BaseColor = new ThemeColors
            {
                Background = Color.FromRgb(0x22, 0x22, 0x33),
                Foreground = Color.FromRgb(0xcc, 0xcc, 0xcc)
            };

            profile.GlobalKeyColor = MetroColors.Cyan;

            profile.TitleBarColor = new ThemeColors
            {
                Background = Color.FromRgb(0x22, 0x22, 0x33),
                Foreground = Color.FromRgb(0xcc, 0xcc, 0xcc)
            };

            profile.BackstageColor = new ThemeColors
            {
                Background = Colors.Black,
                Foreground = Color.FromRgb(0xaa, 0xaa, 0xaa)
            };

            profile.AccountSelectionFlipColor = new ThemeColors
            {
                Background = Color.FromRgb(0x11, 0x11, 0x11),
                Foreground = Color.FromRgb(0xcc, 0xcc, 0xcc)
            };

            profile.SearchFlipColor = new SearchFlipTheme
            {
                Default = new ThemeColors
                {
                    Background = Color.FromRgb(0x1b, 0x58, 0xb8),
                    Foreground = Colors.White
                },
                Pressed = new ThemeColors
                {
                    Background = Color.FromRgb(0x16, 0x49, 0x9a),
                    Foreground = Colors.White
                },
                Selected = new ThemeColors
                {
                    Background = Color.FromRgb(0x5c, 0x83, 0xc2),
                    Foreground = Colors.White
                },
                Hovering = new ThemeColors
                {
                    Background = Color.FromRgb(0x5c, 0x83, 0xc2),
                    Foreground = Colors.White
                },
                QueryInvalid = new ThemeColors
                {
                    Background = MetroColors.Crimson,
                    Foreground = Colors.White
                }
            };

            profile.UserFlipColor = new UserFlipTheme
            {
                Default = new ThemeColors
                {
                    Background = Color.FromRgb(0x11, 0x11, 0x11),
                    Foreground = Colors.White
                },
                Selected = new ThemeColors
                {
                    Background = Color.FromRgb(0x33, 0x33, 0x33),
                    Foreground = Colors.White
                },
                Hovering = new ThemeColors
                {
                    Background = Color.FromRgb(0x22, 0x22, 0x22),
                    Foreground = Colors.White
                },
                Pressed = new ThemeColors
                {

                    Background = Color.FromRgb(0x44, 0x44, 0x44),
                    Foreground = Colors.White
                },
                NotFollowbacked = Colors.DimGray,
                Followbacked = MetroColors.Cyan,
                NotFollowed = Color.FromRgb(0x33, 0x33, 0x33),
                Following = MetroColors.Cyan,
                Blocking = MetroColors.Red
            };

            profile.TabColor = new TabTheme
            {
                TabFont = FontTheme.CreateDefaultWithSize(16.0),
                Default = Color.FromRgb(0x88, 0x88, 0x88),
                Selected = Color.FromRgb(0xbb, 0xbb, 0xbb),
                Focused = Colors.White,
                UnreadCount = Color.FromRgb(0xbb, 0xbb, 0xbb),
            };

            #endregion

            #region status color setting

            var alpha = (Func<Color, byte, Color>)((c, a) =>
            {
                c.A = a;
                return c;
            });

            var foreground = Color.FromRgb(0xcc, 0xcc, 0xcc);
            profile.TweetDefaultColor = CreateDefault(MetroColors.Cyan,
                foreground,
                Colors.Transparent,
                alpha(Colors.Black, 0x10));

            profile.TweetMyselfColor = CreateDefault(MetroColors.Cyan,
                foreground,
                alpha(MetroColors.Cyan, 0x16),
                alpha(MetroColors.Cyan, 0x20));

            profile.TweetMentionColor = CreateDefault(MetroColors.Orange,
                foreground,
                alpha(MetroColors.Orange, 0x16),
                alpha(MetroColors.Orange, 0x20));

            profile.TweetRetweetColor = CreateDefault(MetroColors.Emerald,
                foreground,
                alpha(MetroColors.Emerald, 0x16),
                alpha(MetroColors.Emerald, 0x20));

            profile.TweetDirectMessageColor = CreateDefault(MetroColors.Crimson,
                foreground,
                alpha(MetroColors.Crimson, 0x16),
                alpha(MetroColors.Crimson, 0x20));

            #endregion

            return profile;
        }

        private static TweetTheme CreateDefault(Color key, Color foreground, Color background, Color highlight)
        {
            var dcf = (Func<Color, ControlColors>)(c => new ControlColors
            {
                Default = new ThemeColors
                {
                    Background = Colors.Transparent,
                    Foreground = c,
                },
                Hovering = new ThemeColors
                {
                    Background = Color.FromArgb(0x90, 0xdc, 0xdc, 0xdc),
                    Foreground = c,
                },
                Pressed = new ThemeColors
                {
                    Background = Color.FromArgb(0x90, 0x90, 0x90, 0x90),
                    Foreground = c,
                }
            });
            var dc = dcf(Color.FromRgb(0x80, 0x80, 0x80));

            return new TweetTheme
            {
                Default = new ThemeColors
                {
                    Background = background,
                    Foreground = foreground,
                },
                Highlight = new ThemeColors
                {
                    Background = highlight,
                    Foreground = foreground,
                },
                KeyText = key,
                SubText = Color.FromRgb(0x80, 0x80, 0x80),
                HyperlinkText = Colors.DimGray,
                FavoriteCounter = MetroColors.Amber,
                RetweetCounter = MetroColors.Emerald,
                RetweetMarker = MetroColors.Emerald,

                FavoriteAndRetweetButton = dc,
                FavoriteButton = dc,
                ColoredFavoriteButton = dcf(MetroColors.Amber),
                RetweetButton = dc,
                ColoredRetweetButton = dcf(MetroColors.Emerald),
                MentionButton = dc,
                DeleteButton = dc
            };
        }

        public static string ExportThemeAsXaml(ThemeProfile profile)
        {
            const string resourceDictionaryStart =
                "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">";

            const string resourceDictionaryEnd = "</ResourceDictionary>";

            var rd = profile.CreateResourceDictionary();
            // create resource dictionary
            var content = rd.Keys.OfType<string>()
                            .OrderBy(s => s)
                            .Select(s =>
                            {
                                var res = rd[s];
                                var brush = res as SolidColorBrush;
                                if (brush != null)
                                {
                                    return "<SolidColorBrush x:Key=\"" + s + "\">" +
                                           brush.Color.ToColorString() +
                                           "</SolidColorBrush>";
                                }
                                if (res is Color)
                                {
                                    return "<Color x:Key=\"" + s + "\">" +
                                           ((Color)res).ToColorString() +
                                           "</Color>";
                                }
                                return String.Empty;
                            })
                            .Where(s => !String.IsNullOrEmpty(s))
                            .JoinString(Environment.NewLine);
            return resourceDictionaryStart + Environment.NewLine +
                   content + Environment.NewLine +
                   resourceDictionaryEnd;
        }
    }
}
