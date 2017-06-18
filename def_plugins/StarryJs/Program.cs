using System;
using StarryEyes.Feather.Scripting;

namespace StarryJs
{
    public class Program : IScriptExecutor
    {
        public Guid Id
        {
            get { return new Guid("b83ca033-5d65-4d2b-ba12-f745950337d1"); }
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
            get { return new[] {"js"}; }
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