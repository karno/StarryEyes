using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public class ManagementCrud : CrudBase<DatabaseManagement>
    {
        public ManagementCrud() : base(ResolutionMode.Replace) { }

        private const int AppVersion = 1;
        public string DatabaseVersion
        {
            get { return this.GetValue(AppVersion); }
            set { this.SetValue(AppVersion, value); }
        }

        #region synchronous crud

        private string GetValue(long id)
        {
            var mgmt = this.GetValueCore(id);
            return mgmt == null ? null : mgmt.Value;
        }

        private DatabaseManagement GetValueCore(long id)
        {
            var sql = this.CreateSql("Id = @Id");
            try
            {
                using (this.AcquireReadLock())
                using (var con = this.DangerousOpenConnection())
                {
                    return con.Query<DatabaseManagement>(sql, new { Id = id })
                              .SingleOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw WrapException(ex, "GetValueCore", sql);
            }
        }

        private void SetValue(long id, string value)
        {
            SetValueCore(new DatabaseManagement(id, value));
        }

        private void SetValueCore(DatabaseManagement mgmt)
        {
            try
            {
                using (this.AcquireWriteLock())
                using (var con = this.DangerousOpenConnection())
                using (var tr = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    con.Execute(this.TableInserter, mgmt);
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw WrapException(ex, "SetValueCore", this.TableInserter);
            }
        }

        #endregion

        internal async Task VacuumAsync()
        {
            // should execute WITHOUT transaction.
            await WriteTaskFactory.StartNew(() =>
            {
                try
                {
                    using (AcquireWriteLock())
                    using (var con = this.DangerousOpenConnection())
                    {
                        con.Execute("VACUUM;");
                    }
                }
                catch (Exception ex)
                {
                    throw WrapException(ex, "VacuumAsync", "VACUUM;");
                }
            });
        }
    }
}
