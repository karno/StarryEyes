using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace StarryEyes.Casket.Connections
{
    public interface IDatabaseConnectionDescriptor : IDisposable
    {
        TaskFactory GetTaskFactory(bool isWrite);

        IDisposable AcquireWriteLock();

        IDisposable AcquireReadLock();

        DbConnection GetConnection();
    }

    internal static class DatabaseConnectionHelper
    {
        public static IsolationLevel DefaultIsolationLevel { get; set; }

        static DatabaseConnectionHelper()
        {
            DefaultIsolationLevel = IsolationLevel.Serializable;
        }

        internal static Task<int> ExecuteAsync(this IDatabaseConnectionDescriptor descriptor,
            string query)
        {
            return descriptor
                .GetTaskFactory(true)
                .StartNew(() =>
                {
                    using (descriptor.AcquireWriteLock())
                    {
                        try
                        {
                            using (var con = descriptor.GetConnection())
                            using (var tr = con.BeginTransaction(DefaultIsolationLevel))
                            {
                                var result = con.Execute(query, transaction: tr);
                                tr.Commit();
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "ExecuteAsync", query);
                        }
                    }
                });
        }

        internal static Task<int> ExecuteAsync(this IDatabaseConnectionDescriptor descriptor,
            string query, object param)
        {
            return descriptor
                .GetTaskFactory(true)
                .StartNew(() =>
                {
                    using (descriptor.AcquireWriteLock())
                    {
                        try
                        {
                            // System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                            using (var con = descriptor.GetConnection())
                            using (var tr = con.BeginTransaction(DefaultIsolationLevel))
                            {
                                var result = con.Execute(query, param, tr);
                                tr.Commit();
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "ExecuteAsyncWithParam", query);
                        }
                    }
                });
        }

        internal static Task ExecuteAllAsync(this IDatabaseConnectionDescriptor descriptor,
            IEnumerable<Tuple<string, object>> queryAndParams)
        {
            var qnp = queryAndParams.Memoize();
            return descriptor
                .GetTaskFactory(true)
                .StartNew(() =>
                {
                    using (descriptor.AcquireWriteLock())
                    {
                        try
                        {
                            using (var con = descriptor.GetConnection())
                            using (var tr = con.BeginTransaction(DefaultIsolationLevel))
                            {
                                foreach (var tuple in qnp)
                                {
                                    // tuple := (query, param)
                                    // System.Diagnostics.Debug.WriteLine("EXECUTE: " + qap.Item1);
                                    con.Execute(tuple.Item1, tuple.Item2, tr);
                                }
                                tr.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "ExecuteAllAsync",
                                qnp.Select(q => q.Item1).JoinString(Environment.NewLine));
                        }
                    }
                });
        }

        internal static Task<IEnumerable<T>> QueryAsync<T>(this IDatabaseConnectionDescriptor descriptor,
            string query, object param)
        {
            // System.Diagnostics.Debug.WriteLine("QUERY: " + query);
            return descriptor
                .GetTaskFactory(false)
                .StartNew(() =>
                {
                    using (descriptor.AcquireReadLock())
                    {
                        try
                        {
                            using (var con = descriptor.GetConnection())
                            {
                                return con.Query<T>(query, param);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "QueryAsync", query);
                        }
                    }
                });
        }

        internal static SqliteCrudException WrapException(Exception exception, string command, string query)
        {
            return new SqliteCrudException(exception, command, query);
        }
    }
}