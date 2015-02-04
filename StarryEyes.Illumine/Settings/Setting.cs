
namespace StarryEyes.Illumine.Settings
{
    public class Setting
    {
        private const string DefaultFileName = "krile_i.xml";

        private static Setting _defaultSetting;

        public static Setting Default
        {
            get { return _defaultSetting ?? (_defaultSetting = Load(DefaultFileName, false)); }
        }

        public static Setting Load(string file, bool ignoreBackup)
        {

        }

        public void Save()
        {

        }
    }
}
