using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StarryEyes.Feather.Scripting
{
    /// <summary>
    /// Manage script execution.
    /// </summary>
    public static class ScriptingManager
    {
        private static readonly IDictionary<string, IScriptExecutor> _executors =
            new Dictionary<string, IScriptExecutor>();

        /// <summary>
        /// Register executor for execution scripts.
        /// </summary>
        public static bool RegisterExecutor(string ext, IScriptExecutor executor)
        {
            if (_executors.ContainsKey(ext))
                return false;
            _executors.Add(ext, executor);
            return true;
        }

        public static void ExecuteScripts(string path)
        {
            Directory.CreateDirectory(path).EnumerateFiles()
                .ForEach(file =>
                {
                    var ext = file.Extension.TrimStart('.').ToLower();
                    IScriptExecutor executor;
                    if (_executors.TryGetValue(ext, out executor))
                        executor.ExecuteScript(ext);
                });
        }
    }

    public interface IScriptExecutor
    {
        void ExecuteScript(string filePath);
    }
}
