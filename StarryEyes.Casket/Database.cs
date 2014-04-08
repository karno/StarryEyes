using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
        private static readonly AccountInfoCrud _accountInfoCrud = new AccountInfoCrud();
        private static readonly StatusCrud _statusCrud = new StatusCrud();
        private static readonly StatusEntityCrud _statusEntityCrud = new StatusEntityCrud();
        private static readonly UserCrud _userCrud = new UserCrud();
        private static readonly UserDescriptionEntityCrud _userDescEntityCrud = new UserDescriptionEntityCrud();
        private static readonly UserUrlEntityCrud _userUrlEntityCrud = new UserUrlEntityCrud();
        private static readonly ListCrud _listCrud = new ListCrud();
        private static readonly ListUserCrud _listUserCrud = new ListUserCrud();
        private static readonly FavoritesCrud _favoritesCrud = new FavoritesCrud();
        private static readonly RetweetsCrud _retweetsCrud = new RetweetsCrud();
        private static readonly RelationCrud _relationCrud = new RelationCrud();
        private static readonly ManagementCrud _managementCrud = new ManagementCrud();

        private static string _dbFilePath;
        private static bool _isInitialized;

        public static string DbFilePath { get { return _dbFilePath; } }

        public static AccountInfoCrud AccountInfoCrud
        {
            get { return _accountInfoCrud; }
        }

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

        public static ListCrud ListCrud
        {
            get { return _listCrud; }
        }

        public static ListUserCrud ListUserCrud
        {
            get { return _listUserCrud; }
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
                    AccountInfoCrud.InitializeAsync(),
                    StatusCrud.InitializeAsync(),
                    StatusEntityCrud.InitializeAsync(),
                    UserCrud.InitializeAsync(),
                    UserDescriptionEntityCrud.InitializeAsync(),
                    UserUrlEntityCrud.InitializeAsync(),
                    ListCrud.InitializeAsync(),
                    ListUserCrud.InitializeAsync(),
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
            var batch = new StatusInsertBatch
            {
                Status = status,
                StatusEntities = statusEntities,
                User = user,
                UserDescriptionEntities = userDescriptionEntities,
                UserUrlEntities = userUrlEntities
            };
            await StoreStatuses(new[] { batch });
        }


        public static async Task StoreStatuses(IEnumerable<StatusInsertBatch> batches)
        {
            try
            {
                await StatusCrud.StoreCoreAsync(batches.SelectMany(CreateQuery));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("# FAIL> storing statuses." + Environment.NewLine + ex);
            }
        }

        public static async Task StoreUser(DatabaseUser user,
            IEnumerable<DatabaseUserDescriptionEntity> userDescriptionEntities,
            IEnumerable<DatabaseUserUrlEntity> userUrlEntities)
        {
            var batch = new UserInsertBatch
            {
                User = user,
                UserDescriptionEntities = userDescriptionEntities,
                UserUrlEntities = userUrlEntities
            };
            await StoreUsers(new[] { batch });
        }

        public static async Task StoreUsers(IEnumerable<UserInsertBatch> batches)
        {
            try
            {
                await StatusCrud.StoreCoreAsync(batches.SelectMany(CreateQuery));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("# FAIL> storing users." + Environment.NewLine + ex);
            }
        }

        #endregion

        private static IEnumerable<Tuple<string, object>> CreateQuery(StatusInsertBatch batch)
        {
            return EnumerableEx.Concat(
                new[] { Tuple.Create(_statusInserter, (object)batch.Status) },
                new[] { _statusEntityCrud.CreateDeleter(batch.Status.Id) },
                batch.StatusEntities.Select(e => Tuple.Create(_statusEntityInserter, (object)e)))
                               .Concat(CreateQuery(batch.UserInsertBatch));
        }

        private static IEnumerable<Tuple<string, object>> CreateQuery(UserInsertBatch batch)
        {
            return EnumerableEx.Concat(
                new[] { Tuple.Create(_userInserter, (object)batch.User) },
                new[] { _userDescEntityCrud.CreateDeleter(batch.User.Id) },
                batch.UserDescriptionEntities.Select(e => Tuple.Create(_userDescEntityInserter, (object)e)),
                new[] { _userUrlEntityCrud.CreateDeleter(batch.User.Id) },
                batch.UserUrlEntities.Select(e => Tuple.Create(_userUrlEntityInserter, (object)e))
                );
        }

        public static async Task VacuumTables()
        {
            await _managementCrud.VacuumAsync();
        }
    }

    public class StatusInsertBatch
    {
        public static StatusInsertBatch CreateBatch(
            Tuple<DatabaseStatus, IEnumerable<DatabaseStatusEntity>> status,
            Tuple<DatabaseUser, IEnumerable<DatabaseUserDescriptionEntity>, IEnumerable<DatabaseUserUrlEntity>> user)
        {
            return new StatusInsertBatch
            {
                Status = status.Item1,
                StatusEntities = status.Item2,
                User = user.Item1,
                UserDescriptionEntities = user.Item2,
                UserUrlEntities = user.Item3
            };
        }

        public StatusInsertBatch()
        {
            UserInsertBatch = new UserInsertBatch();
        }

        public DatabaseStatus Status { get; set; }

        public IEnumerable<DatabaseStatusEntity> StatusEntities { get; set; }

        public UserInsertBatch UserInsertBatch { get; set; }

        public DatabaseUser User
        {
            get { return UserInsertBatch.User; }
            set { UserInsertBatch.User = value; }
        }

        public IEnumerable<DatabaseUserDescriptionEntity> UserDescriptionEntities
        {
            get { return UserInsertBatch.UserDescriptionEntities; }
            set { UserInsertBatch.UserDescriptionEntities = value; }
        }

        public IEnumerable<DatabaseUserUrlEntity> UserUrlEntities
        {
            get { return UserInsertBatch.UserUrlEntities; }
            set { UserInsertBatch.UserUrlEntities = value; }
        }
    }

    public class UserInsertBatch
    {
        public static UserInsertBatch CreateBatch(
            Tuple<DatabaseUser, IEnumerable<DatabaseUserDescriptionEntity>, IEnumerable<DatabaseUserUrlEntity>> user)
        {
            return new UserInsertBatch
            {
                User = user.Item1,
                UserDescriptionEntities = user.Item2,
                UserUrlEntities = user.Item3
            };
        }

        public DatabaseUser User { get; set; }

        public IEnumerable<DatabaseUserDescriptionEntity> UserDescriptionEntities { get; set; }

        public IEnumerable<DatabaseUserUrlEntity> UserUrlEntities { get; set; }
    }
}
