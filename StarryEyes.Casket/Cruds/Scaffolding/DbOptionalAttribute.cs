using System;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    [AttributeUsage(AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = true)]
    public class DbOptionalAttribute : Attribute
    {
    }
}
