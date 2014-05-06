using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Feather.Scripting;

namespace StarryEyes.Plugins
{
    public class ScriptingManagerImpl : ScriptingManager
    {
        internal static void Initialize()
        {
            // initialize from app core
            var instance = new ScriptingManagerImpl();
            var prop = typeof(ScriptingManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            prop.SetValue(null, instance);
            instance.ExecuteScripts();
        }

        private readonly ConcurrentDictionary<string, IScriptExecutor> _executors =
            new ConcurrentDictionary<string, IScriptExecutor>();
        private readonly ConcurrentDictionary<string, IScriptExecutor> _executorExtResolver =
            new ConcurrentDictionary<string, IScriptExecutor>();

        private ScriptingManagerImpl()
        {
        }

        private void ExecuteScripts()
        {
            var targetPath = Path.Combine(App.ExeFileDir, App.ScriptDirectiory);
            if (Directory.Exists(targetPath))
            {
                foreach (var file in Directory.GetFiles(targetPath, "*", SearchOption.TopDirectoryOnly))
                {
                    this.ExecuteFile(file);
                }
            }
        }

        public override bool RegisterExecutor([NotNull] IScriptExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException("executor");
            if (_executors.ContainsKey(executor.Name)) return false;
            _executors[executor.Name] = executor;
            executor.Extensions
                    .Where(ext => !this._executorExtResolver.ContainsKey(ext))
                    .ForEach(ext => this._executorExtResolver[ext] = executor);
            return true;
        }

        public override IEnumerable<IScriptExecutor> Executors
        {
            get { return _executors.Values.ToArray(); }
        }

        public override IScriptExecutor GetExecutor(string executorName)
        {
            IScriptExecutor executor;
            return this._executors.TryGetValue(executorName, out executor)
                       ? executor
                       : null;
        }

        public override bool ExecuteFile([NotNull] string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");
            var ext = (Path.GetExtension(filePath) ?? "").Trim(new[] { '.', ' ' });
            IScriptExecutor executor;
            if (_executorExtResolver.TryGetValue(ext, out executor))
            {
                Task.Run(() => executor.Execute(File.ReadAllText(filePath)));
                return true;
            }
            return false;
        }
    }
}
