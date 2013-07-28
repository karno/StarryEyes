
namespace StarryEyes.Casket.DatabaseModels.Generators
{
    public abstract class DbModelBase
    {
        private string _tableName;
        public string TableName
        {
            get { return _tableName ?? (_tableName = DbSentenceGenerator.GetTableName(this.GetType())); }
        }

        private string _tableCreator;
        public string TableCreator
        {
            get { return _tableCreator ?? (_tableCreator = DbSentenceGenerator.GetTableCreator(this.GetType())); }
        }

        private string _tableInsertor;
        public string TableInserter
        {
            get { return _tableInsertor ?? (_tableInsertor = DbSentenceGenerator.GetTableInserter(this.GetType())); }
        }

        private string _tableUpdator;
        public string TableUpdator
        {
            get { return _tableUpdator ?? (_tableUpdator = DbSentenceGenerator.GetTableUpdater(this.GetType())); }
        }

        private string _tableDeletor;
        public string TableDeletor
        {
            get { return _tableDeletor ?? (_tableDeletor = DbSentenceGenerator.GetTableDeleter(this.GetType())); }
        }
    }
}
