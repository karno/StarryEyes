using System;

namespace StarryEyes.Casket.DatabaseCore.Sqlite
{
    [AttributeUsage(AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = true)]
    public class DbOptionalAttribute : Attribute
    {
    }
}
