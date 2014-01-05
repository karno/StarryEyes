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

            profile.GlobalFont = FontTheme.Default;

            profile.BaseColor = new ThemeColors
            {
                Background = Colors.White,
                Foreground = Colors.Black
            };

            profile.GlobalKeyColor = new ThemeColors
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

            profile.TweetDefaultColor = CreateDefault(MetroColors.Cyan, Colors.Transparent,
                                                 alpha(Colors.Black, 0x10));

            profile.TweetMyselfColor = CreateDefault(MetroColors.Cyan,
                                                alpha(MetroColors.Cyan, 0x16),
                                                alpha(MetroColors.Cyan, 0x20));

            profile.TweetMentionColor = CreateDefault(MetroColors.Orange,
                                                alpha(MetroColors.Orange, 0x16),
                                                alpha(MetroColors.Orange, 0x20));

            profile.TweetRetweetColor = CreateDefault(MetroColors.Emerald,
                                                alpha(MetroColors.Emerald, 0x16),
                                                alpha(MetroColors.Emerald, 0x20));

            profile.TweetDirectMessageColor = CreateDefault(MetroColors.Crimson,
                                                alpha(MetroColors.Crimson, 0x16),
                                                alpha(MetroColors.Crimson, 0x20));

            #endregion

            return profile;
        }

        private static TweetTheme CreateDefault(Color key, Color background, Color highlight)
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
                    Background = Color.FromRgb(0xdc, 0xdc, 0xdc),
                    Foreground = c,
                },
                Pressed = new ThemeColors
                {
                    Background = Color.FromRgb(0x90, 0x90, 0x90),
                    Foreground = c,
                }
            });
            var dc = dcf(Color.FromRgb(0x80, 0x80, 0x80));

            return new TweetTheme
            {
                Default = new ThemeColors
                {
                    Background = background,
                    Foreground = Colors.Black,
                },
                Highlight = new ThemeColors
                {
                    Background = highlight,
                    Foreground = Colors.Black,
                },
                KeyText = key,
                SubText = Color.FromRgb(0x80, 0x80, 0x80),
                FavoriteCounter = MetroColors.Amber,
                RetweetCounter = MetroColors.Emerald,
                RetweetMarker = MetroColors.Emerald,

                FavoriteAndRetweetButton = dc,
                FavoriteButton = dc,
                ColoredFavoriteButton = dcf(MetroColors.Amber),
                ColoredRetweetButton = dc,
                RetweetButton = dcf(MetroColors.Emerald),
                MentionButton = dc,
                DeleteButton = dc
            };
        }
    }
}
