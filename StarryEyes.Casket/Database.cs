using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Casket.Scaffolds.Generators;

namespace StarryEyes.Casket
{
    public static class Database
    {
        private static readonly StatusCrud _statusCrud = new StatusCrud();
        private static readonly UserCrud _userCrud = new UserCrud();
        private static readonly EntityCrud _entityCrud = new EntityCrud();
        private static readonly FavoritesCrud _favoritesCrud = new FavoritesCrud();
        private static readonly RetweetsCrud _retweetsCrud = new RetweetsCrud();
        private static readonly MaintenanceCrud _maintenanceCrud = new MaintenanceCrud();

        private static string _dbFilePath;
        private static bool _isInitialized;

        public static string DbFilePath { get { return _dbFilePath; } }

        public static StatusCrud StatusCrud
        {
            get { return _statusCrud; }
        }

        public static UserCrud UserCrud
        {
            get { return _userCrud; }
        }

        public static EntityCrud EntityCrud
        {
            get { return _entityCrud; }
        }

        public static FavoritesCrud FavoritesCrud
        {
            get { return _favoritesCrud; }
        }

        public static RetweetsCrud RetweetsCrud
        {
            get { return _retweetsCrud; }
        }

        public static void Initialize(string dbFilePath)
        {
            System.Diagnostics.Debug.WriteLine("Krile DB Initializing...(" + dbFilePath + ")");
            if (_isInitialized)
            {
                throw new InvalidOperationException("Database core is already initialized.");
            }
            _isInitialized = true;
            _dbFilePath = dbFilePath;
            var tasks = new Task[] { };
            Task.WaitAll(Task.Factory.StartNew(() =>
            {
                tasks = new[]
                {
                    StatusCrud.InitializeAsync(),
                    UserCrud.InitializeAsync(),
                    EntityCrud.InitializeAsync(),
                    FavoritesCrud.InitializeAsync(),
                    RetweetsCrud.InitializeAsync()
                };
            }));
            Task.WaitAll(tasks);
        }

        #region store in one transaction

        private static readonly string _statusInserter = SentenceGenerator.GetTableInserter<DatabaseStatus>();
        private static readonly string _userInserter = SentenceGenerator.GetTableInserter<DatabaseUser>(true);
        private static readonly string _entityInserter = SentenceGenerator.GetTableInserter<DatabaseEntity>();

        public static async Task StoreStatus(DatabaseStatus status,
                                              DatabaseUser user,
                                              IEnumerable<DatabaseEntity> entities)
        {
            try
            {
                await StatusCrud.StoreCoreAsync(new[]
                {
                    Tuple.Create(_statusInserter, (object) status),
                    Tuple.Create(_userInserter, (object) user),
                }.Concat(entities.Select(e => Tuple.Create(_entityInserter, (object)e))));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        #endregion

        public static async Task VacuumTables()
        {
            await _maintenanceCrud.ExecuteAsync("vacuum;");
        }
    }
}
