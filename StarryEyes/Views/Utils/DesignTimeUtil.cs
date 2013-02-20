using System.ComponentModel;
using System.Windows;

namespace StarryEyes.Views.Utils
{
    public static class DesignTimeUtil
    {
        private static bool? _isInDesignMode;
        public static bool IsInDesignMode
        {
            get
            {
                return _isInDesignMode ??
                       (_isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject())).Value;
            }
        }
    }
}
