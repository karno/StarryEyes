using System;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = true)]
    public class DbNameAttribute : Attribute
    {
        private readonly string _name;

        public DbNameAttribute()
        {
        }

        public DbNameAttribute(string name)
        {
            this._name = name;
        }

        public string Name
        {
            get { return this._name; }
        }
    }
}
