using System;
using System.Reflection;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace StarryEyes.Hotfixes
{

    /// <summary>
    /// Fix behavior around WM_NCHITTEST.
    /// </summary>
    /// <remarks>
    /// See issue 135: https://github.com/karno/StarryEyes/issues/135
    /// </remarks>
    public class HitTestHotfixBehavior : Behavior<Window>
    {
        private const int WM_NCHITTEST = 0x0084;

        private static readonly DependencyProperty HitTestHotfixAppliedProperty = DependencyProperty.RegisterAttached(
            "HitTestHotfixApplied", typeof(bool), typeof(HitTestHotfixBehavior), new PropertyMetadata(default(bool)));

        private HwndSource _source;

        protected override Freezable CreateInstanceCore()
        {
            return this;
        }

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += OnWindowLoaded;
            base.OnAttached();
        }

        void OnWindowLoaded(object sender, EventArgs e)
        {
            ((Window)sender).Loaded -= OnWindowLoaded;
            if (AssociatedObject == null) return;
            var window = AssociatedObject;
            if ((bool)window.GetValue(HitTestHotfixAppliedProperty)) return;
            _source = HwndSource.FromHwnd(new WindowInteropHelper(AssociatedObject).EnsureHandle());
            if (_source != null)
            {
                _source.AddHook(SnatchProc);
                System.Diagnostics.Debug.WriteLine("Snatch procedure registered.");
                window.SetValue(HitTestHotfixAppliedProperty, true);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (!(bool)AssociatedObject.GetValue(HitTestHotfixAppliedProperty)) return;
            if (_source != null)
            {
                _source.RemoveHook(SnatchProc);
                AssociatedObject.ClearValue(HitTestHotfixAppliedProperty);
            }
        }

        private IntPtr SnatchProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                // round 64bit lparam to 32bit
                var nlparam = unchecked((int)lparam.ToInt64());
                var nlpptr = new IntPtr(nlparam);
                System.Diagnostics.Debug.WriteLine("rounded lparam: " + lparam.ToInt64() + " to " + nlpptr.ToInt64());
                var result = CallInternalHitTest(msg, wparam, nlpptr, out handled);
                handled = true; // prevent calling original WM_NCHITTEST handler
                return result;
            }
            return IntPtr.Zero;
        }

        private IntPtr CallInternalHitTest(int msg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Call:
            // internal static WindowChromeWorker System.Windows.Shell.WindowChromeWorker.GetWindowChromeWorker(Window) 
            var assembly = typeof(System.Windows.Shell.WindowChrome).Assembly;
            var wcwType = assembly.GetType("System.Windows.Shell.WindowChromeWorker");
            var methodInfo = wcwType.GetMethod("GetWindowChromeWorker", BindingFlags.Public | BindingFlags.Static);
            var worker = methodInfo.Invoke(null, new object[] { AssociatedObject });
            if (worker == null)
            {
                handled = false;
                return IntPtr.Zero;
            }
            // Call:
            // private IntPtr _HandleNCHitTest(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
            // WM is enum:int
            var hittest = wcwType.GetMethod("_HandleNCHitTest", BindingFlags.NonPublic | BindingFlags.Instance);
            var param = new object[] { msg, wParam, lParam, null };
            var result = hittest.Invoke(worker, param);
            // getting out param 
            handled = (bool)param[3];
            return (IntPtr)result;
        }
    }
}
