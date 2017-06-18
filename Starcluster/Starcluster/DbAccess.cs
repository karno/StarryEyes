namespace Starcluster
{
    public static class DbAccess
    {
        public static void RegisterExtensionFunctions()
        {
            // currently not supported, future feature...
            // (perhaps we can implement this by pulling code from system.data.sqlite)
            /*
            // Assembly.GetExecutingAssembly() is not available for .NET Standard 1.3, 
            // then we have to write bit tricky code:
            // https://stackoverflow.com/questions/41943984/assembly-getexecutingassembly-available-in-net-core
            var asm = typeof(DbAccess).GetTypeInfo().Assembly;
            var functions = asm.DefinedTypes
                               .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SqliteFunction)))
                               .Where(t => t.GetCustomAttribute<SqliteFunctionAttribute>() != null);
            foreach (var f in functions)
            {
                try
                {
                    SqliteFunction.RegisterFunction(f);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("RegisterFunction failed: " + f.FullName, ex);
                }
            }
            */
        }
    }
}