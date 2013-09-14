using System;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DbPrimaryKeyAttribute : Attribute
    {
        private readonly bool _isAutoIncrement;
        public DbPrimaryKeyAttribute()
            : this(false)
        {
        }

        public DbPrimaryKeyAttribute(bool isAutoIncrement)
        {
            this._isAutoIncrement = isAutoIncrement;
        }

        public bool IsAutoIncrement
        {
            get { return this._isAutoIncrement; }
        }
    }
}
