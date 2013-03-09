using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarryEyes.Settings
{
    /// <summary>
    /// Manage Key Binding profiles.
    /// </summary>
    public static class KeyBindingManager
    {
        private static void Load()
        {
        }

        public static event Action OnKeyBindingChanged;

        private static void RaiseKeyBindingChanged()
        {
            Action handler = OnKeyBindingChanged;
            if (handler != null) handler();
        }
    }
}
