using System.Collections.Generic;

namespace StarryEyes.Feather.Scripting
{
    /// <summary>
    /// Manage script execution.
    /// </summary>
    public abstract class ScriptingManager
    {
        #region inject from krile core

        public static ScriptingManager Instance { get; private set; }

        #endregion

        public abstract bool RegisterExecutor(string ext, IScriptExecutor executor);

        public abstract IEnumerable<string> RegisteredExecutors { get; }

        public abstract void Execute(string executorName, string script, params object[] parameters);

        public abstract T Evaluate<T>(string executorName, string script, params object[] parameters);

        public abstract void ExecuteFile(string filePath);

        public abstract void ExecuteFile(string executorName, string filePath);
    }
}
