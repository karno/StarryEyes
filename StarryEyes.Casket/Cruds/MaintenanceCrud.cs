using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.Cruds
{
    internal sealed class MaintenanceCrud : CrudBase
    {
        internal async Task VacuumAsync()
        {
            await this.ExecuteAsync("vacuum;");
        }
    }
}
