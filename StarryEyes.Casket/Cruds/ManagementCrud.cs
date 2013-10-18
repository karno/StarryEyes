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
            using (var _ = this.AcquireReadLock())
            using (var con = this.OpenConnection())
            {
                return con.Query<DatabaseManagement>(
                    this.CreateSql("Id = @Id"), new { Id = id })
                          .SingleOrDefault();
            }
        }

        private void SetValue(long id, string value)
        {
            SetValueCore(new DatabaseManagement(id, value));
        }

        private void SetValueCore(DatabaseManagement mgmt)
        {
            using (var _ = this.AcquireWriteLock())
            using (var con = this.OpenConnection())
            using (var tr = con.BeginTransaction())
            {
                con.Execute(this.TableInserter, mgmt);
                tr.Commit();
            }
        }

        #endregion

        internal async Task VacuumAsync()
        {
            await this.ExecuteAsync("vacuum;");
        }
    }
}
