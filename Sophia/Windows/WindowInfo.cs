using System.Windows;

namespace Sophia.Windows
{
    public sealed class WindowInfo
    {
        public bool Activate { get; set; }

        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public Rect Bounds
        {
            get { return new Rect(new Point(Left, Top), new Size(Width, Height)); }
            set
            {
                Left = value.Left;
                Top = value.Top;
                Width = value.Width;
                Height = value.Height;
            }
        }

        public WindowState? State { get; set; }

        public WindowInfo(bool activate = false)
        {
            Activate = activate;
            Left = Top = Width = Height = double.NaN;
            State = null;
            Activate = activate;
        }

        public WindowInfo(Rect rect, WindowState state, bool activate = false)
        {
            Bounds = rect;
            State = state;
            Activate = activate;
        }


        public WindowInfo(double left, double top, double width, double height,
            WindowState state, bool activate = false)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            State = state;
            Activate = activate;
        }

        public void Apply(Window window)
        {
            if (!double.IsNaN(Left)) window.Left = Left;
            if (!double.IsNaN(Top)) window.Top = Top;
            if (!double.IsNaN(Width)) window.Width = Width;
            if (!double.IsNaN(Height)) window.Height = Height;
            if (State != null) window.WindowState = State.Value;
            if (Activate)
            {
                window.Activate();
            }
        }
    }
}