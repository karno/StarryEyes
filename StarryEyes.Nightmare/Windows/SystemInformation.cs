using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows
{
    public static class SystemInformation
    {
        public static string ComputerName
        {
            get { return WinForms.SystemInformation.ComputerName; }
        }

        public static string UserName
        {
            get { return WinForms.SystemInformation.UserName; }
        }

        public static bool MouseButtonsSwapped
        {
            get { return WinForms.SystemInformation.MouseButtonsSwapped; }
        }

        public static int MouseWheelScrollDelta
        {
            get { return WinForms.SystemInformation.MouseWheelScrollDelta; }
        }
    }
}
