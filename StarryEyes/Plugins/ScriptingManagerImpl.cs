using System.Collections.Generic;
using System.Reflection;
using StarryEyes.Feather.Scripting;

namespace StarryEyes.Plugins
{
    public class ScriptingManagerImpl : ScriptingManager
    {
        public static void Initialize()
        {
            var prop = typeof(ScriptingManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic);
            prop.SetValue(null, new ScriptingManagerImpl());
        }

        private ScriptingManagerImpl()
        {
            // initialize from core module
        }

        public override bool RegisterExecutor(string ext, IScriptExecutor executor)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<string> RegisteredExecutors
        {
            get { throw new System.NotImplementedException(); }
        }

        public override void Execute(string executorName, string script, params object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public override T Evaluate<T>(string executorName, string script, params object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public override void ExecuteFile(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public override void ExecuteFile(string executorName, string filePath)
        {
            throw new System.NotImplementedException();
        }
    }
}
