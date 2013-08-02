
using StarryEyes.Casket.DatabaseCore.Sqlite;

namespace StarryEyes.Casket.DatabaseModels
{
    public abstract class DbModelBase
    {
        private string _tableName;
        public string TableName
        {
            get { return this._tableName ?? (this._tableName = SentenceGenerator.GetTableName(this.GetType())); }
        }

        private string _tableCreator;
        public string TableCreator
        {
            get { return this._tableCreator ?? (this._tableCreator = SentenceGenerator.GetTableCreator(this.GetType())); }
        }

        private string _tableInsertor;
        public string TableInserter
        {
            get { return this._tableInsertor ?? (this._tableInsertor = SentenceGenerator.GetTableInserter(this.GetType())); }
        }

        private string _tableUpdator;
        public string TableUpdator
        {
            get { return this._tableUpdator ?? (this._tableUpdator = SentenceGenerator.GetTableUpdater(this.GetType())); }
        }

        private string _tableDeletor;
        public string TableDeletor
        {
            get { return this._tableDeletor ?? (this._tableDeletor = SentenceGenerator.GetTableDeleter(this.GetType())); }
        }
    }
}
