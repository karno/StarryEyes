using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StarryEyes.Models.Plugins
{
    /// <summary>
    /// Manage script execution.
    /// </summary>
    public static class ScriptingManager
    {
        private static SortedDictionary<string, IScriptExecutor> executors =
            new SortedDictionary<string, IScriptExecutor>();

        /// <summary>
        /// Register executor for execution scripts.
        /// </summary>
        public static bool RegisterExecutor(string ext, IScriptExecutor executor)
        {
            if (executors.ContainsKey(ext))
                return false;
            executors.Add(ext, executor);
            return true;
        }

        internal static void ExecuteScripts()
        {
            var path = Path.Combine(Path.GetDirectoryName(App.ExeFilePath), App.ScriptDirectiory);
            Directory.GetFiles(path)
                .ForEach(file =>
                {
                    var ext = Path.GetExtension(file).TrimStart('.').ToLower();
                    IScriptExecutor executor;
                    if (executors.TryGetValue(ext, out executor))
                        executor.ExecuteScript(ext);
                });
        }
    }

    public interface IScriptExecutor
    {
        void ExecuteScript(string filePath);
    }
}
