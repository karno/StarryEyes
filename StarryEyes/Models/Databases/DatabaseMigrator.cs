using System;
using System.Threading.Tasks;
using System.Windows;
using StarryEyes.Casket;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Views.Dialogs;

namespace StarryEyes.Models.Databases
{
    public static class DatabaseMigrator
    {
        private static void MigrateWorkCore(Func<Action<string>, Task> migration)
        {
            // change application shutdown mode for preventing 
            // auto-exit when optimization is completed.
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                // run database optimization
                var optDlg = new WorkingWindow(
                    "migrating database...",
                    migration);
                optDlg.ShowDialog();
            }
            finally
            {
                // restore shutdown mode
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
        }

        /// <summary>
        /// Migrate to database version A
        /// </summary>
        public static void MigrateToVersionA()
        {
            MigrateWorkCore(updateLabel => Task.Run(async () =>
            {
                const string tempTableName = "TEMP_Status";

                updateLabel("checking database...");

                // drop table before migration (preventing errors).
                await Database.ExecuteAsync("DROP TABLE IF EXISTS " + tempTableName + ";");

                updateLabel("optimizing...");

                // vacuuming table
                await Database.VacuumTables();

                updateLabel("preparing for migration...");

                await Database.StatusCrud.AlterAsync(tempTableName);

                // re-create table
                await Database.ReInitializeAsync(Database.StatusCrud);

                updateLabel("migrating database...");

                // insert all records
                await Database.ExecuteAsync(
                    "INSERT INTO Status(Id, BaseId, RetweetId, RetweetOriginalId, StatusType, UserId, BaseUserId, RetweeterId, RetweetOriginalUserId, EntityAidedText, Text, CreatedAt, BaseSource, Source, InReplyToStatusId, InReplyToOrRecipientUserId, InReplyToOrRecipientScreenName, Longitude, Latitude) " +
                    "SELECT Id, BaseId, RetweetId, RetweetOriginalId, StatusType, UserId, BaseUserId, RetweeterId, RetweetOriginalUserId, Text, Text, CreatedAt, Source, Source, InReplyToStatusId, InReplyToOrRecipientUserId, InReplyToOrRecipientScreenName, Longitude, Latitude " +
                    "FROM " + tempTableName + ";");

                updateLabel("cleaning up...");

                await Database.ExecuteAsync("DROP TABLE " + tempTableName + ";");

                await Database.VacuumTables();
            }));
        }

        /// <summary>
        /// Migrate to database version C
        /// </summary>
        public static void MigrateToVersionB()
        {
            MigrateWorkCore(updateLabel => Task.Run(async () =>
            {
                const string tempTableName = "TEMP_Status";

                updateLabel("checking database...");

                // drop table before migration (preventing errors).
                await Database.ExecuteAsync("DROP TABLE IF EXISTS " + tempTableName + ";");

                updateLabel("optimizing...");

                // vacuuming table
                await Database.VacuumTables();

                updateLabel("preparing for migration...");

                await Database.StatusCrud.AlterAsync(tempTableName);

                // re-create table
                await Database.ReInitializeAsync(Database.StatusCrud);

                updateLabel("migrating database...");

                // insert all records
                await Database.ExecuteAsync(
                    "INSERT INTO Status(Id, BaseId, RetweetId, RetweetOriginalId, QuoteId, StatusType, UserId, BaseUserId, RetweeterId, RetweetOriginalUserId, QuoteUserId, EntityAidedText, Text, CreatedAt, BaseSource, Source, InReplyToStatusId, InReplyToOrRecipientUserId, InReplyToOrRecipientScreenName, Longitude, Latitude, DisplayTextRangeBegin, DisplayTextRangeEnd) " +
                    " SELECT Id, BaseId, RetweetId, RetweetOriginalId,    NULL, StatusType, UserId, BaseUserId, RetweeterId, RetweetOriginalUserId,        NULL, EntityAidedText, Text, CreatedAt, BaseSource, Source, InReplyToStatusId, InReplyToOrRecipientUserId, InReplyToOrRecipientScreenName, Longitude, Latitude, NULL, NULL " +
                    " FROM " + tempTableName + ";");

                updateLabel("cleaning up...");

                await Database.ExecuteAsync("DROP TABLE " + tempTableName + ";");

                await Database.VacuumTables();
            }));
        }

        public static void MigrateToVersionC()
        {
            MigrateWorkCore(updateLabel => Task.Run(async () =>
            {
                const string statusEntityTable = "StatusEntity";
                const string userDescriptionEntityTable = "UserDescriptionEntity";
                const string userUrlEntityTable = "UserUrlEntity";
                var tables = new[] { statusEntityTable, userDescriptionEntityTable, userUrlEntityTable };

                int step = 1;
                await MigrateToVersionCInternal(statusEntityTable, Database.StatusEntityCrud,
                    $"step {step}/{tables.Length} :", updateLabel);
                step++;
                await MigrateToVersionCInternal(userDescriptionEntityTable, Database.UserDescriptionEntityCrud,
                    $"step {step}/{tables.Length} :", updateLabel);
                step++;
                await MigrateToVersionCInternal(userUrlEntityTable, Database.UserUrlEntityCrud,
                    $"step {step}/{tables.Length} :", updateLabel);
                step++;
            }));
        }

        private static async Task MigrateToVersionCInternal<T>(string tableName, CrudBase<T> crudProvider,
            string step, Action<string> updateLabel) where T : class
        {
            var tempTableName = "TEMP_" + tableName;
            updateLabel(step + "checking database...");

            // drop table before migration (preventing errors).
            await Database.ExecuteAsync("DROP TABLE IF EXISTS " + tempTableName + ";");

            updateLabel(step + "optimizing...");

            // vacuuming table
            await Database.VacuumTables();

            updateLabel(step + "preparing for migration...");

            await crudProvider.AlterAsync(tempTableName);

            // re-create table
            await Database.ReInitializeAsync(crudProvider);

            updateLabel(step + "migrating database...");

            // insert all records
            await Database.ExecuteAsync(
                $"INSERT INTO {tableName}(Id, ParentId, EntityType, DisplayText, OriginalUrl, UserId, MediaUrl, StartIndex, EndIndex, MediaType) " +
                " SELECT Id, ParentId, EntityType, DisplayText, OriginalUrl, UserId, MediaUrl, StartIndex, EndIndex, NULL " +
                $" FROM {tempTableName};");

            updateLabel(step + "cleaning up...");

            await Database.ExecuteAsync("DROP TABLE " + tempTableName + ";");

            await Database.VacuumTables();
        }
    }
}