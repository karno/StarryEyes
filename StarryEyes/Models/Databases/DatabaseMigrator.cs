
using System;
using System.Threading.Tasks;
using System.Windows;
using StarryEyes.Casket;
using StarryEyes.Views.Dialogs;

namespace StarryEyes.Models.Databases
{
    public static class DatabaseMigrator
    {
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
    }
}
