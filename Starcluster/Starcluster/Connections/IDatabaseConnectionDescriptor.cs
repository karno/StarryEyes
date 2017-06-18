using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Starcluster.Connections
{
    public interface IDatabaseConnectionDescriptor : IDisposable
    {
        TaskFactory GetTaskFactory(bool isWrite);

        IDisposable AcquireWriteLock();

        IDisposable AcquireReadLock();

        IDbConnection GetConnection();
    }

    public static class DatabaseConnectionHelper
    {
        public static IsolationLevel DefaultIsolationLevel { get; set; }

        static DatabaseConnectionHelper()
        {
            DefaultIsolationLevel = IsolationLevel.Serializable;
        }

        public static Task<int> ExecuteAsync(
            this IDatabaseConnectionDescriptor descriptor, string query)
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
                                try
                                {
                                    var result = con.Execute(query, transaction: tr);
                                    tr.Commit();
                                    return result;
                                }
                                catch
                                {
                                    tr.Rollback();
                                    throw;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "ExecuteAsync", query);
                        }
                    }
                }, TaskCreationOptions.DenyChildAttach);
        }

        public static Task<int> ExecuteAsync(
            this IDatabaseConnectionDescriptor descriptor, string query, object param)
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
                                try
                                {
                                    var result = con.Execute(query, param, tr);
                                    tr.Commit();
                                    return result;
                                }
                                catch
                                {
                                    tr.Rollback();
                                    throw;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "ExecuteAsyncWithParam", query);
                        }
                    }
                }, TaskCreationOptions.DenyChildAttach);
        }

        public static Task ExecuteAllAsync(
            this IDatabaseConnectionDescriptor descriptor, IEnumerable<Tuple<string, object>> queryAndParams)
        {
            var qnp = queryAndParams.ToArray();
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
                                try
                                {
                                    foreach (var tuple in qnp)
                                    {
                                        // tuple := (query, param)
                                        // System.Diagnostics.Debug.WriteLine("EXECUTE: " + qap.Item1);
                                        con.Execute(tuple.Item1, tuple.Item2, tr);
                                    }
                                    tr.Commit();
                                }
                                catch
                                {
                                    tr.Rollback();
                                    throw;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw WrapException(ex, "ExecuteAllAsync",
                                String.Join(Environment.NewLine, qnp.Select(q => q.Item1)));
                        }
                    }
                }, TaskCreationOptions.DenyChildAttach);
        }

        public static Task<IEnumerable<T>> QueryAsync<T>(
            this IDatabaseConnectionDescriptor descriptor, string query, object param)
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
                }, TaskCreationOptions.DenyChildAttach);
        }

        private static DatabaseAccessException WrapException(Exception exception, string command, string query)
        {
            return new DatabaseAccessException(exception, command, query);
        }
    }
}