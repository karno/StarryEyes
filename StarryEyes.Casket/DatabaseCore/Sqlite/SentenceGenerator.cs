using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using StarryEyes.Casket.DatabaseModels.Generators;

namespace StarryEyes.Casket.DatabaseCore.Sqlite
{
    public static class SentenceGenerator
    {
        private static readonly Dictionary<Type, string> TypeMapping =
        new Dictionary<Type, string>{
            {typeof(String), "TEXT"},
            {typeof(Int32), "INT"},
            {typeof(Int64), "INT"},
            {typeof(Single), "REAL"},
            {typeof(Double), "REAL"},
            {typeof(Decimal), "REAL"},
            {typeof(Boolean), "BOOLEAN"},
            {typeof(Enum), "INT"},
            {typeof(DateTime), "DATETIME"},
        };

        public static string GetTableName<T>()
        {
            return GetTableName(typeof(T));
        }

        public static string GetTableName(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(DbNameAttribute), false)
                           .OfType<DbNameAttribute>()
                           .FirstOrDefault();
            return attr != null ? attr.Name : type.Name;
        }

        public static string GetTableCreator<T>()
        {
            return GetTableCreator(typeof(T));
        }

        public static string GetTableCreator(Type type)
        {
            var builder = new StringBuilder();
            builder.Append("CREATE TABLE IF NOT EXISTS ");
            builder.Append(GetTableName(type));
            builder.Append("(");
            var first = true;
            foreach (var prop in type.GetProperties())
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;
                // name
                var name = prop.GetDbNameOfProperty();

                // type & nullable
                var isNullable = false;
                var ptype = prop.PropertyType;
                if (ptype.IsGenericType &&
                    ptype.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    isNullable = true;
                    ptype = ptype.GetGenericArguments().First();
                }
                else if (prop.GetCustomAttributes(typeof(DbOptionalAttribute), false).Any())
                {
                    isNullable = true;
                }
                if (typeof(Enum).IsAssignableFrom(ptype))
                {
                    ptype = typeof(Enum);
                }
                var typeStr = TypeMapping[ptype];

                // build attributes
                var attrs = "";
                var pk = prop.GetTypedCustomAttribute<DbPrimaryKeyAttribute>();
                if (pk != null)
                {
                    if (isNullable)
                    {
                        throw new ArgumentException("Primary key is not nullable.");
                    }
                    attrs += " PRIMARY KEY" +
                             (pk.IsAutoIncrement ? " AUTOINCREMENT" : "");
                }
                else if (!isNullable)
                {
                    attrs = " NOT NULL";
                }
                builder.Append(name + " " + typeStr + attrs);
            }
            builder.Append(");");
            return builder.ToString();
        }

        public static string GetTableInserter<T>()
        {
            return GetTableInserter(typeof(T));
        }

        public static string GetTableInserter(Type type, bool replaceOnConflict = false)
        {
            var builder = new StringBuilder();
            var values = new StringBuilder();
            builder.Append(replaceOnConflict ? "INSERT OR REPLACE INTO " : "INSERT INTO ");
            builder.Append(GetTableName(type));
            builder.Append("(");
            values.Append(" VALUES ");
            values.Append("(");
            var first = true;
            foreach (var prop in type.GetProperties())
            {
                var pk = prop.GetTypedCustomAttribute<DbPrimaryKeyAttribute>();
                if (pk != null && pk.IsAutoIncrement)
                {
                    // ignore auto increment primary key
                    continue;
                }
                if (!first)
                {
                    builder.Append(", ");
                    values.Append(", ");
                }
                first = false;
                builder.Append(prop.GetDbNameOfProperty());
                values.Append("@" + prop.Name);
            }
            builder.Append(")");
            values.Append(");");
            builder.Append(values);
            return builder.ToString();
        }

        public static string GetTableUpdater<T>()
        {
            return GetTableUpdater(typeof(T));
        }

        public static string GetTableUpdater(Type type)
        {
            var builder = new StringBuilder();
            builder.Append("UPDATE ");
            builder.Append(GetTableName(type));
            builder.Append(" SET ");
            var first = true;
            var pk = "";
            foreach (var prop in type.GetProperties())
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;
                builder.Append(prop.GetDbNameOfProperty());
                builder.Append(" = ");
                builder.Append("@" + prop.Name);
                if (prop.GetCustomAttributes(typeof(DbPrimaryKeyAttribute), false).Any())
                {
                    pk = prop.GetDbNameOfProperty() + " = @" + prop.Name;
                }
            }
            builder.Append(" WHERE ");
            builder.Append(pk);
            builder.Append(";");
            return builder.ToString();
        }

        public static string GetTableDeleter<T>()
        {
            return GetTableDeleter(typeof(T));
        }

        public static string GetTableDeleter(Type type)
        {
            var builder = new StringBuilder();
            builder.Append("DELETE ");
            builder.Append(GetTableName(type));
            builder.Append(" WHERE ");
            type.GetProperties()
                .Where(prop => prop.GetCustomAttributes(typeof(DbPrimaryKeyAttribute), false)
                                   .Any())
                .Take(1)
                .ForEach(prop => builder.Append(prop.GetDbNameOfProperty() + " = @" + prop.Name));
            builder.Append(";");
            return builder.ToString();
        }

        private static string GetDbNameOfProperty(this PropertyInfo prop)
        {
            var pn = prop.GetTypedCustomAttribute<DbNameAttribute>();
            if (pn != null && !String.IsNullOrEmpty(pn.Name))
            {
                return pn.Name;
            }
            return prop.Name;
        }

        private static T GetTypedCustomAttribute<T>(this PropertyInfo prop)
        {
            return prop.GetCustomAttributes(typeof(T), false)
                       .OfType<T>()
                       .FirstOrDefault();
        }
    }
}
