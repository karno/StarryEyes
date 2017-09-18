using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows
{
    public static class Application
    {
        public static void EnableVisualStyles()
        {
            WinForms.Application.EnableVisualStyles();
        }

        public static void Restart()
        {
            WinForms.Application.Restart();
        }
    }
}