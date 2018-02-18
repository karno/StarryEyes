using System.Collections.Concurrent;
using System.IO;
using StarryEyes.Configuration.ConfigurationItems;

namespace StarryEyes.Configuration
{
    public class Configuration
    {
        private bool _prepared = false;

        private static ConcurrentDictionary<string, object> _settingValueHolder =
            new ConcurrentDictionary<string, object>();

        public ConfigurationItem GetValue(string key)
        {
        }

        public bool SetValue(string key, ConfigurationItem value)
        {
        }

        public bool Save()
        {
            if (!_prepared) return;
            // two-step writeback to prevent file corruption
            var tempfn = Path.Combine(App.ConfigurationDirectoryPath, Path.GetRandomFileName());
            using (var fs = File.Open(tempfn, FileMode.Create, FileAccess.ReadWrite))
            {
                // save to temp file
            }

            // validate file


            File.Move(tempfn, App.ConfigurationFilePath);
        }
    }
}