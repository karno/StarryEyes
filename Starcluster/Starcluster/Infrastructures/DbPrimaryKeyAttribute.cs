using System;

namespace Starcluster.Infrastructures
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbPrimaryKeyAttribute : Attribute
    {
        public DbPrimaryKeyAttribute(bool isAutoIncrement = false)
        {
            IsAutoIncrement = isAutoIncrement;
        }

        public bool IsAutoIncrement { get; }
    }
}