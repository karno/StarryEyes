using System;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DbUniqueColumnAttribute : Attribute
    {
        private readonly string[] _columns;

        public DbUniqueColumnAttribute(string column)
            : this(new[] { column })
        {

        }

        public DbUniqueColumnAttribute(params string[] columns)
        {
            this._columns = columns;
        }

        public string[] Columns
        {
            get { return this._columns; }
        }
    }
}
