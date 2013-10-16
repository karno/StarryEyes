using System;
using StarryEyes.Feather.ConcreteInterfaces;

namespace StarryJs
{
    public class Program : IScriptExecutor
    {
        public string Id
        {
            get { return "Jurassic"; }
        }

        public string Name
        {
            get { return "StarryJs"; }
        }

        public Version Version
        {
            get { return new Version(1, 0, 0); }
        }

        public Uri SupportUri
        {
            get { return new Uri("http://krile.starwing.net/"); }
        }

        public void Initialize()
        {
        }

        public string ScriptName
        {
            get { return "ECMAScript"; }
        }

        public string[] Extensions
        {
            get { return new[] { "js" }; }
        }

        public void Execute(string script, params object[] parameters)
        {
            var engine = new Jurassic.ScriptEngine();
            int i = 0;
            foreach (var p in parameters)
            {
                engine.SetGlobalValue("parameter" + i, p);
                i++;
            }
            engine.Execute(script);
        }

        public T Evaluate<T>(string script, params object[] parameters)
        {
            var engine = new Jurassic.ScriptEngine();
            int i = 0;
            foreach (var p in parameters)
            {
                engine.SetGlobalValue("parameter" + i, p);
                i++;
            }
            return engine.Evaluate<T>(script);
        }
    }
}
