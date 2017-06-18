using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public class ManagementTable : TableBase<DbManagement>
    {
        public ManagementTable(string tableName) : base(tableName, ResolutionMode.Replace)
        {
        }

        private const int DbVersionId = 1;

        public string DbVersion
        {
            get => GetValue(DbVersionId);
            set => SetValue(DbVersionId, value);
        }

        #region synchronous crud

        private string GetValue(long id)
        {
            var mgmt = GetValueCore(id);
            return mgmt?.Value;
        }

        private DbManagement GetValueCore(long id)
        {
            // synchronized read
            var sql = CreateSql("Id = @Id");
            try
            {
                using (Descriptor.AcquireReadLock())
                using (var con = Descriptor.GetConnection())
                {
                    return con.Query<DbManagement>(sql, new { Id = id })
                              .SingleOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseAccessException(ex, "GetValueCore", sql);
            }
        }

        private void SetValue(long id, string value)
        {
            SetValueCore(new DbManagement(id, value));
        }

        private void SetValueCore(DbManagement mgmt)
        {
            // synchronized insert
            try
            {
                using (Descriptor.AcquireWriteLock())
                using (var con = Descriptor.GetConnection())
                using (var tr = con.BeginTransaction(DatabaseConnectionHelper.DefaultIsolationLevel))
                {
                    con.Execute(TableInserter, mgmt);
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseAccessException(ex, "SetValueCore", TableInserter);
            }
        }

        #endregion synchronous crud

        public async Task VacuumAsync()
        {
            await Descriptor
                .GetTaskFactory(true)
                .StartNew(() =>
                {
                    // should execute WITHOUT transaction.
                    try
                    {
                        using (Descriptor.AcquireWriteLock())
                        using (var con = Descriptor.GetConnection())
                        {
                            con.Execute("VACUUM;");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new DatabaseAccessException(ex, "VacuumAsync", TableInserter);
                    }
                });
        }
    }
}