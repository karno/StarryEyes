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

        public abstract bool RegisterExecutor(IScriptExecutor executor);

        public abstract IEnumerable<IScriptExecutor> Executors { get; }

        public abstract IScriptExecutor GetExecutor(string executorName);

        public abstract bool ExecuteFile(string filePath);
    }
}
