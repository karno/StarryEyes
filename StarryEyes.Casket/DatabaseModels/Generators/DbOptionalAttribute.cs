using System;

namespace StarryEyes.Casket.DatabaseModels.Generators
{
    [AttributeUsage(AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = true)]
    public class DbOptionalAttribute : Attribute
    {
    }
}
