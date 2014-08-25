using ClipboardTemplates.models;
using ClipboardTemplates.models.TemplatesDataSetTableAdapters;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;


namespace ClipboardTemplates.models
{
 
    /// <summary>
    /// This class should verify database and prepare it for TemplatesDataSet
    /// </summary>
    public class TemplatesDatabase : IDisposable
    {

        protected string _file;
        protected SQLiteConnection _connection;

        // Used trick - when you add one listerer you don't have to check event to nullity
        public event EventHandler ConnectionClosed = delegate { };
        public event EventHandler ChangesSaved = delegate { };

        public string DatabaseName
        {
            get
            {
                if (_connection == null)
                {
                    throw new Exception("No database name, when no database connection.");
                };
                return this._connection.DataSource;
            }
        }

        public TemplatesDatabase(string file, bool truncateDatabase = false)
        {
            if (truncateDatabase)
            {
                var fi = new FileInfo(file);
                if (fi.Exists)
                {
                    fi.Delete();
                }
            }

            this._file = file;
            this._connection = new SQLiteConnection(string.Format("data source={0}", file));
            this._connection.Open();
            if (truncateDatabase == true)
            {
                this.setupDatabase();
            }
            this.checkDatabase();
        }

        private void setupDatabase()
        {
            var q = System.Text.Encoding.UTF8.GetString(Properties.Resources.setupDB);
            //Console.WriteLine(q);
            // CAUTION! UTF-8 BOM
            this.ExecuteNonQuery(q);
        }


        protected void checkDatabase() {
            // Just quick over look; should be more precise
            this.DoesTableExists("Templates");
            this.DoesTableExists("TemplateFields");
        }

        protected TemplatesDataSet dataSet;
        public TemplatesDataSet DataSet
        {
            get { 
                if (this.dataSet == null) 
                    this.dataSet = new TemplatesDataSet(); 
                return dataSet;
            }
            //set { dataSet = value; }
        }

        protected TableAdapterManager tableAdapterManager;
        public TableAdapterManager TableAdapterManager
        {
            get {
                var m = this.tableAdapterManager;
                var c = this._connection;

                if (m == null)
                {
                    this.tableAdapterManager = m = new TableAdapterManager();
                    m.Connection = c;
                    m.TemplatesTableAdapter = new TemplatesTableAdapter();
                    m.TemplatesTableAdapter.Connection = c;
                    m.TemplateFieldsTableAdapter = new TemplateFieldsTableAdapter();
                    m.TemplateFieldsTableAdapter.Connection = c;
                }
                return tableAdapterManager;
            }
            //set { tableAdapterManager = value; }
        }

        /// <summary>
        /// Updates database based on changes made in dataset
        /// </summary>
        public void SaveChanges()
        {
            this.TableAdapterManager.UpdateAll(this.DataSet);
            this.ChangesSaved(this,new EventArgs());
        }




        


























        protected void DoesTableExists(String table)
        {
            string name = this.ExecuteScalar(
                string.Format("SELECT name FROM sqlite_master WHERE type='table' AND name='{0}';", table)
            );
            if (name == null)
            {
                throw new Exception("Table " + table + " does not exists");
            }
        }

        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        public int ExecuteNonQuery(string sql)
        {
            SQLiteCommand mycommand = new SQLiteCommand(this._connection);
            mycommand.CommandText = sql;
            int rowsUpdated = mycommand.ExecuteNonQuery();
            return rowsUpdated;
        }

        /// <summary>
        ///     Allows the programmer to retrieve single items from the DB.
        /// </summary>
        /// <param name="sql">The query to run.</param>
        /// <returns>A string.</returns>
        public string ExecuteScalar(string sql)
        {
            SQLiteCommand mycommand = new SQLiteCommand(this._connection);
            mycommand.CommandText = sql;
            object value = mycommand.ExecuteScalar();
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        public void CloseConnection()
        {
            if (this._connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;

                // Hack
                // @see http://stackoverflow.com/questions/8511901/system-data-sqlite-close-not-releasing-database-file
                GC.Collect();

                // fire event
                this.ConnectionClosed(this, new EventArgs());
            }
        }

        public void Dispose()
        {
            this.CloseConnection();
        }
    }

}
