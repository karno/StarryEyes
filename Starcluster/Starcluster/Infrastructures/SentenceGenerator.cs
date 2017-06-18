using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Starcluster.Infrastructures
{
    public static class SentenceGenerator
    {
        private static readonly Dictionary<Type, string> TypeMapping =
            new Dictionary<Type, string>
            {
                {typeof(String), "TEXT"},
                {typeof(Int32), "INTEGER"},
                {typeof(Int64), "INTEGER"},
                {typeof(Single), "REAL"},
                {typeof(Double), "REAL"},
                {typeof(Decimal), "REAL"},
                {typeof(Boolean), "BOOLEAN"},
                {typeof(Enum), "INT"},
                {typeof(DateTime), "DATETIME"}
            };

        public static string GetTableName<T>()
        {
            return GetTableName(typeof(T));
        }

        public static string GetTableName(Type type)
        {
            var attr = type.GetTypeInfo()
                           .GetCustomAttributes(typeof(DbNameAttribute), false)
                           .OfType<DbNameAttribute>()
                           .FirstOrDefault();
            return attr != null ? attr.Name : type.Name;
        }

        public static string GetTableCreator<T>(string tableName = null)
        {
            return GetTableCreator(typeof(T), tableName);
        }

        public static string GetTableCreator(Type type, string tableName = null)
        {
            var builder = new StringBuilder();
            builder.Append("CREATE TABLE IF NOT EXISTS ");
            builder.Append(tableName ?? GetTableName(type));
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
                if (ptype.GetTypeInfo().IsGenericType &&
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
            var uniques = type.GetTypeInfo().GetCustomAttributes<DbUniqueColumnAttribute>();
            foreach (var attr in uniques)
            {
                builder.Append(", UNIQUE(" + String.Join(", ", attr.Columns) + ")");
            }
            builder.Append(");");
            return builder.ToString();
        }

        public static string GetTableInserter<T>(string tableName = null,
            ResolutionMode onConflict = ResolutionMode.Abort)
        {
            return GetTableInserter(typeof(T), tableName, onConflict);
        }

        public static string GetTableInserter(Type type, string tableName = null,
            ResolutionMode onConflict = ResolutionMode.Abort)
        {
            var builder = new StringBuilder();
            var values = new StringBuilder();
            builder.Append("INSERT ");
            switch (onConflict)
            {
                case ResolutionMode.Abort:
                    break;

                case ResolutionMode.Rollback:
                    builder.Append("OR ROLLBACK ");
                    break;

                case ResolutionMode.Fail:
                    builder.Append("OR FAIL ");
                    break;

                case ResolutionMode.Ignore:
                    builder.Append("OR IGNORE ");
                    break;

                case ResolutionMode.Replace:
                    builder.Append("OR REPLACE ");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(onConflict));
            }
            builder.Append("INTO ");
            builder.Append(tableName ?? GetTableName(type));
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

        public static string GetTableUpdater<T>(string tableName = null)
        {
            return GetTableUpdater(typeof(T), tableName);
        }

        public static string GetTableUpdater(Type type, string tableName = null)
        {
            var builder = new StringBuilder();
            builder.Append("UPDATE ");
            builder.Append(tableName ?? GetTableName(type));
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

        public static string GetTableDeleter<T>(string tableName = null)
        {
            return GetTableDeleter(typeof(T), tableName);
        }

        public static string GetTableDeleter(Type type, string tableName = null)
        {
            var builder = new StringBuilder();
            builder.Append("DELETE FROM ");
            builder.Append(tableName ?? GetTableName(type));
            builder.Append(" WHERE ");
            var primaryKey = type.GetProperties()
                                 .First(prop => prop.GetCustomAttributes(typeof(DbPrimaryKeyAttribute), false)
                                                    .Any());
            builder.Append(primaryKey.GetDbNameOfProperty() + " = @" + primaryKey.Name);
            builder.Append(";");
            return builder.ToString();
        }

        public static string GetTableAlterer(Type type, IEnumerable<string> existColumns, string tableName = null)
        {
            var exist = existColumns.Select(n => n.ToLower()).ToArray();
            var builder = new StringBuilder();
            foreach (var prop in type.GetProperties())
            {
                // name
                var name = prop.GetDbNameOfProperty();
                if (exist.Contains(name.ToLower())) continue; // existed

                builder.Append("ALTER TABLE ");
                builder.Append(tableName ?? GetTableName(type));
                builder.Append(" ADD ");

                // type & nullable
                var isNullable = false;
                var ptype = prop.PropertyType;
                if (ptype.GetTypeInfo().IsGenericType &&
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
                builder.Append(";");
            }
            return builder.ToString();
        }

        private static string GetDbNameOfProperty(this PropertyInfo prop)
        {
            var pn = prop.GetTypedCustomAttribute<DbNameAttribute>();
            return !String.IsNullOrEmpty(pn?.Name) ? pn.Name : prop.Name;
        }

        private static T GetTypedCustomAttribute<T>(this PropertyInfo prop)
        {
            return prop.GetCustomAttributes(typeof(T), false)
                       .OfType<T>()
                       .FirstOrDefault();
        }
    }

    public enum ResolutionMode
    {
        Abort,
        Rollback,
        Fail,
        Ignore,
        Replace
    }
}