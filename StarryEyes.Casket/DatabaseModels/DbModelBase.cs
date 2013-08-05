
using StarryEyes.Casket.DatabaseCore.Sqlite;

namespace StarryEyes.Casket.DatabaseModels
{
    public abstract class DbModelBase
    {
        protected virtual bool ReplaceOnConflict
        {
            get { return true; }
        }

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

        private string _tableInserter;
        public string TableInserter
        {
            get { return this._tableInserter ?? (this._tableInserter = SentenceGenerator.GetTableInserter(this.GetType(), ReplaceOnConflict)); }
        }

        private string _tableUpdater;
        public string TableUpdator
        {
            get { return this._tableUpdater ?? (this._tableUpdater = SentenceGenerator.GetTableUpdater(this.GetType())); }
        }

        private string _tableDeleter;
        public string TableDeletor
        {
            get { return this._tableDeleter ?? (this._tableDeleter = SentenceGenerator.GetTableDeleter(this.GetType())); }
        }
    }
}
