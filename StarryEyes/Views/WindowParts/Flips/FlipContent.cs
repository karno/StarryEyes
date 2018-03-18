using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Livet;
using StarryEyes.Views.Utils;

namespace StarryEyes.Views.WindowParts.Flips
{
    public class FlipContent : ContentControl
    {
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(FlipContent),
                new PropertyMetadata(false, OnOpenChanged));

        private static void OnOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var fc = sender as FlipContent;
            if (fc == null || e.NewValue == e.OldValue) return;
            if ((bool)e.NewValue)
            {
                fc.OwnerVisibility = Visibility.Visible;
                if (!VisualStateManager.GoToState(fc, "Open", true))
                {
                    fc.IsOpen = false;
                }
            }
            else
            {
                VisualStateManager.GoToState(fc, "Close", true);
                var timer = new DispatcherTimer(DispatcherPriority.Loaded, DispatcherHelper.UIDispatcher)
                {
                    Interval = TimeSpan.FromSeconds(0.2)
                };
                timer.Tick += (o, args) =>
                {
                    timer.Stop();
                    fc.OwnerVisibility = Visibility.Collapsed;
                };
                timer.Start();
            }
        }

        public Visibility OwnerVisibility
        {
            get => (Visibility)GetValue(OwnerVisibilityProperty);
            set => SetValue(OwnerVisibilityProperty, value);
        }

        // Using a DependencyProperty as the backing store for OwnerVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OwnerVisibilityProperty =
            DependencyProperty.Register("OwnerVisibility", typeof(Visibility), typeof(FlipContent),
                new PropertyMetadata(Visibility.Visible));

        public FlipContent()
        {
            Loaded += (sender, args) =>
            {
                if (DesignTimeUtil.IsInDesignMode)
                {
                    VisualStateManager.GoToState(this, "Open", false);
                    OwnerVisibility = Visibility.Visible;
                }
                else
                {
                    VisualStateManager.GoToState(this, "Close", false);
                    OwnerVisibility = Visibility.Collapsed;
                }
            };
        }
    }
}