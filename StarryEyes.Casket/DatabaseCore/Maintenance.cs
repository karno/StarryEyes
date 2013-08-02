using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Casket.DatabaseCore.Sqlite;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.DatabaseCore
{
    public static class Maintenance
    {
        public static async Task InitializeTables()
        {
            var refs = new Dictionary<ReferencingTable, string>
            {
                {ReferencingTable.Status, new DatabaseStatus().TableCreator},
                {ReferencingTable.User, new DatabaseUser().TableCreator},
                {ReferencingTable.Entity, new DatabaseEntity().TableCreator},
                {ReferencingTable.Favorites, new DatabaseActivity().TableCreator},
                {ReferencingTable.Retweets, new DatabaseActivity().TableCreator}
            };

            foreach (var table in refs)
            {
                var accessor = new Accessor(table.Key);
                await accessor.ExecuteAsync(table.Value);
            }
        }

        public static async Task VacuumTables()
        {
            foreach (var type in Enum.GetValues(typeof(ReferencingTable)))
            {
                var accessor = new Accessor((ReferencingTable)type);
                await accessor.ExecuteAsync("vacuum;");
            }
        }
    }
}
