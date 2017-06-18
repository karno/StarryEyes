using System;

namespace Starcluster.Infrastructures
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
    public class DbNameAttribute : Attribute
    {
        public DbNameAttribute()
        {
        }

        public DbNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}