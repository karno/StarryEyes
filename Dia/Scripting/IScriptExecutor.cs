
namespace StarryEyes.Feather.Scripting
{
    public interface IScriptExecutor : IPlugin
    {
        string ScriptName { get; }

        string[] Extensions { get; }

        void Execute(string script, params object[] parameters);

        T Evaluate<T>(string script, params object[] parameters);
    }
}