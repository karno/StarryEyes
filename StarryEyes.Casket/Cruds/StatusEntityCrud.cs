using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class StatusEntityCrud : EntityCrudBase<DatabaseStatusEntity>
    {
        protected override string IndexPrefix
        {
            get { return "SET"; }
        }

        public async Task DeleteNotExistsAsync(string statusTableName)
        {
            await Descriptor.ExecuteAsync(
                string.Format(
                    "DELETE FROM {0} WHERE NOT EXISTS (SELECT Id FROM {1} " +
                    "WHERE {1}.Id = {0}.ParentId);",
                    this.TableName, statusTableName));
        }
    }
}
