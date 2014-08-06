using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Casket.Connections;
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
            // synchronized read
            var sql = this.CreateSql("Id = @Id");
            try
            {
                using (Descriptor.AcquireReadLock())
                using (var con = Descriptor.GetConnection())
                {
                    return con.Query<DatabaseManagement>(sql, new { Id = id })
                              .SingleOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw DatabaseConnectionHelper.WrapException(ex, "GetValueCore", sql);
            }
        }

        private void SetValue(long id, string value)
        {
            SetValueCore(new DatabaseManagement(id, value));
        }

        private void SetValueCore(DatabaseManagement mgmt)
        {
            // synchronized insert
            try
            {
                using (Descriptor.AcquireWriteLock())
                using (var con = Descriptor.GetConnection())
                using (var tr = con.BeginTransaction(DatabaseConnectionHelper.DefaultIsolationLevel))
                {
                    con.Execute(this.TableInserter, mgmt);
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw DatabaseConnectionHelper.WrapException(ex, "SetValueCore", this.TableInserter);
            }
        }

        #endregion

        internal async Task VacuumAsync()
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
                        using (var tr = con.BeginTransaction(DatabaseConnectionHelper.DefaultIsolationLevel)
                            )
                        {
                            con.Execute("VACUUM;");
                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw DatabaseConnectionHelper.WrapException(ex, "VacuumAsync", this.TableInserter);
                    }
                });
        }
    }
}
