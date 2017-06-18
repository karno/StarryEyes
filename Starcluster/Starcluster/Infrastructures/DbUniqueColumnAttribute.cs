using System;

namespace Starcluster.Infrastructures
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DbUniqueColumnAttribute : Attribute
    {
        public DbUniqueColumnAttribute(string column)
            : this(new[] { column })
        {
        }

        public DbUniqueColumnAttribute(params string[] columns)
        {
            Columns = columns;
        }

        public string[] Columns { get; }
    }
}