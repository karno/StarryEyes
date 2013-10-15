using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket
{
    public static class Database
    {
        private static readonly StatusCrud _statusCrud = new StatusCrud();
        private static readonly StatusEntityCrud _statusEntityCrud = new StatusEntityCrud();
        private static readonly UserCrud _userCrud = new UserCrud();
        private static readonly UserDescriptionEntityCrud _userDescEntityCrud = new UserDescriptionEntityCrud();
        private static readonly UserUrlEntityCrud _userUrlEntityCrud = new UserUrlEntityCrud();
        private static readonly FavoritesCrud _favoritesCrud = new FavoritesCrud();
        private static readonly RetweetsCrud _retweetsCrud = new RetweetsCrud();
        private static readonly RelationCrud _relationCrud = new RelationCrud();
        private static readonly ManagementCrud _managementCrud = new ManagementCrud();

        private static string _dbFilePath;
        private static bool _isInitialized;

        public static string DbFilePath { get { return _dbFilePath; } }

        public static StatusCrud StatusCrud
        {
            get { return _statusCrud; }
        }

        public static StatusEntityCrud StatusEntityCrud
        {
            get { return _statusEntityCrud; }
        }

        public static UserCrud UserCrud
        {
            get { return _userCrud; }
        }

        public static UserDescriptionEntityCrud UserDescriptionEntityCrud
        {
            get { return _userDescEntityCrud; }
        }

        public static UserUrlEntityCrud UserUrlEntityCrud
        {
            get { return _userUrlEntityCrud; }
        }

        public static FavoritesCrud FavoritesCrud
        {
            get { return _favoritesCrud; }
        }

        public static RetweetsCrud RetweetsCrud
        {
            get { return _retweetsCrud; }
        }

        public static RelationCrud RelationCrud
        {
            get { return _relationCrud; }
        }

        public static ManagementCrud ManagementCrud
        {
            get { return _managementCrud; }
        }

        public static void Initialize(string dbFilePath)
        {
            System.Diagnostics.Debug.WriteLine("Krile DB Initializing...(" + dbFilePath + ")");
            if (_isInitialized)
            {
                throw new InvalidOperationException("Database core is already initialized.");
            }
            _isInitialized = true;

            // register sqlite functions
            var asm = Assembly.GetExecutingAssembly();
            asm.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SQLiteFunction)))
               .Where(t => t.GetCustomAttribute<SQLiteFunctionAttribute>() != null)
               .ForEach(SQLiteFunction.RegisterFunction);

            // initialize tables
            _dbFilePath = dbFilePath;
            var tasks = new Task[] { };
            Task.WaitAll(Task.Factory.StartNew(() =>
            {
                tasks = new[]
                {
                    StatusCrud.InitializeAsync(),
                    StatusEntityCrud.InitializeAsync(),
                    UserCrud.InitializeAsync(),
                    UserDescriptionEntityCrud.InitializeAsync(),
                    UserUrlEntityCrud.InitializeAsync(),
                    FavoritesCrud.InitializeAsync(),
                    RetweetsCrud.InitializeAsync(),
                    RelationCrud.InitializeAsync(),
                    ManagementCrud.InitializeAsync()
                };
            }));
            Task.WaitAll(tasks);
        }

        #region store in one transaction

        private static readonly string _statusInserter = SentenceGenerator.GetTableInserter<DatabaseStatus>();

        private static readonly string _userInserter =
            SentenceGenerator.GetTableInserter<DatabaseUser>(onConflict: ResolutionMode.Replace);

        private static readonly string _statusEntityInserter =
            SentenceGenerator.GetTableInserter<DatabaseStatusEntity>();

        private static readonly string _userDescEntityInserter =
            SentenceGenerator.GetTableInserter<DatabaseUserDescriptionEntity>();

        private static readonly string _userUrlEntityInserter =
            SentenceGenerator.GetTableInserter<DatabaseUserUrlEntity>();

        public static async Task StoreStatus(
            DatabaseStatus status,
            IEnumerable<DatabaseStatusEntity> statusEntities,
            DatabaseUser user,
            IEnumerable<DatabaseUserDescriptionEntity> userDescriptionEntities,
            IEnumerable<DatabaseUserUrlEntity> userUrlEntities)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                await StatusCrud.StoreCoreAsync(
                    EnumerableEx.Concat(
                        new[]
                        {
                            Tuple.Create(_statusInserter, (object) status),
                            Tuple.Create(_userInserter, (object) user)
                        },
                        new[] { _statusEntityCrud.CreateDeleter(status.Id) },
                        statusEntities.Select(e => Tuple.Create(_statusEntityInserter, (object)e)),
                        new[] { _userDescEntityCrud.CreateDeleter(user.Id) },
                        userDescriptionEntities.Select(e => Tuple.Create(_userDescEntityInserter, (object)e)),
                        new[] { _userUrlEntityCrud.CreateDeleter(user.Id) },
                        userUrlEntities.Select(e => Tuple.Create(_userUrlEntityInserter, (object)e))
                        ));
                sw.Stop();
                System.Diagnostics.Debug.WriteLine("INSERT total: " + sw.ElapsedMilliseconds + " msec.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        #endregion

        public static async Task VacuumTables()
        {
            await _managementCrud.VacuumAsync();
        }
    }
}
