using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;



namespace ClipboardTemplates.models
{
    public class SQLiteDatabase
    {
        protected String connectionString;

        protected SQLiteConnection conn = null;
        public SQLiteConnection Connection
        {
            get { return this.conn; }
        }

        public string DatabaseName
        {
            get { 
                if (!Connected) {
                    throw new Exception("No database name, when no database connection.");
                };
                return this.conn.DataSource; 
            }
        }

        public Boolean Connected {
            get { return this.IsConnected(); }
        }

        // Used trick - when you add one listerer you don't have to check event to nullity
        public event EventHandler ConnectionOpened = delegate { };
        public event EventHandler ConnectionClosed = delegate { };












        #region contructing connection string
 
        /// <summary>
        ///     Single Param Constructor for specifying the DB file.
        /// </summary>
        /// <param name="inputFile">The File containing the DB</param>
        public void setDatabaseFile(String inputFile)
        {
            //this.filePath = inputFile;
            // TODO: generate connection string just before connection
            this.connectionString = String.Format("Data Source={0}", inputFile);
        }
        
        #endregion















        #region Connection management

        public void OpenConnection()
        {
            if (Connected) throw new Exception("Cannot create new connection when already connected to another database. Close connection before you call OpenConnection().");

            SQLiteConnection cnn = new SQLiteConnection(connectionString);
            cnn.Open();
            this.conn = cnn;

            // fire event
            this.ConnectionOpened(this, new EventArgs());
        }

        public SQLiteConnection GetConnection() {
            if (!this.Connected)
            {
                this.OpenConnection();
            }
            return this.conn;
        }

        public void CloseConnection()
        {
            if (Connected)
            {
                this.conn.Close();
                this.conn.Dispose();
                this.conn = null;

                // Hack
                // @see http://stackoverflow.com/questions/8511901/system-data-sqlite-close-not-releasing-database-file
                GC.Collect();

                // fire event
                this.ConnectionClosed(this, new EventArgs());
            }
        }

        /// <summary>
        /// Only formal check! This method do not try to ping database.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected() {
            return this.conn != null && (this.conn.State == ConnectionState.Open || this.conn.State == ConnectionState.Executing || this.conn.State == ConnectionState.Fetching);
        }

        #endregion











        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        //public DataTable GetDataTable(string sql)
        //{
        //    DataTable ds = new DataTable();
        //    SQLiteCommand mycommand = new SQLiteCommand(this.GetConnection());
        //    mycommand.CommandText = sql;
        //    SQLiteDataReader reader = mycommand.ExecuteReader();
        //    ds.Load(reader);
        //    reader.Close();
        //    return ds;
        //}

        /// <summary>
        /// Getts read-only data table
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable GetDataTable(string sql)
        {
            return this.GetDataTable(sql, null);
        }

        /// <summary>
        /// Getts writable data table
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="sourceTable"></param>
        /// <returns></returns>
        public DataTable GetDataTable(string sql, string sourceTable) {
            DataSet ds = this.GetDataSet(sql, sourceTable);
            //if (ds.Tables.Count != 1) {
            //    throw new Exception("Cannot get datatable from dataset with other number of tables than one.");
            //}
            return ds.Tables[0];
        }

        public DataSet GetDataSet(string sql, string sourceTable) {
            DataSet ds = new DataSet();
            SQLiteDataAdapter adapter = this.GetDataAdapter(sql);
            if (sourceTable != null) {
                adapter.FillSchema(ds, SchemaType.Source, sourceTable);
            }
            adapter.Fill(ds, sourceTable);
            return ds;
        }

        public SQLiteDataAdapter GetDataAdapter(string sql, Boolean setupCommandBuilder = true) {
            var adapter = new SQLiteDataAdapter(sql, this.conn);

            // or alternatively (same as above)
            //var adapter = new SQLiteDataAdapter();
            //adapter.SelectCommand = new SQLiteCommand(sql, this.conn);
            
            // We want adapter to be writable
            // We need to set these properties
            //
            //adapter.UpdateCommand
            //adapter.InsertCommand;
            //adapter.DeleteCommand
            // 

            // In order you want to truncateDatabase adapter manualy, continue to this link:
            // http://msdn.microsoft.com/cs-cz/library/33y2221y(v=vs.71).aspx

            // In this app, we will use only one table, so take advantage of CommandBuilder
            // Something to read: http://msdn.microsoft.com/cs-cz/library/tf579hcz(v=vs.71).aspx

            // In order to use CommandBuilder, we need to use:
            // - select return primary column
            // - select will be executed; affects performace
            // - Great! When database was changed before I call update DBConcurrencyException is thrown.
            // - Only for stand-alone tables; no foreign keys!

            // Make adapter writable using CommandBuilder
            if (setupCommandBuilder)
            {
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
                adapter.UpdateCommand = builder.GetUpdateCommand();
                adapter.InsertCommand = builder.GetInsertCommand();
                adapter.DeleteCommand = builder.GetDeleteCommand();
            }

            return adapter;
        }

        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        public int ExecuteNonQuery(string sql)
        {
            SQLiteCommand mycommand = new SQLiteCommand(this.GetConnection());
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
            SQLiteCommand mycommand = new SQLiteCommand(this.GetConnection());
            mycommand.CommandText = sql;
            object value = mycommand.ExecuteScalar();
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        /// <summary>
        ///     Allows the programmer to easily update rows in the DB.
        /// </summary>
        /// <param name="tableName">The table to update.</param>
        /// <param name="data">A dictionary containing Column names and their new values.</param>
        /// <param name="where">The where clause for the update statement.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Update(String tableName, Dictionary<String, String> data, String where)
        {
            String vals = "";
            Boolean returnCode = true;
            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, String> val in data)
                {
                    vals += String.Format(" {0} = '{1}',", val.Key.ToString(), val.Value.ToString());
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", tableName, vals, where));
            }
            catch
            {
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily delete rows from the DB.
        /// </summary>
        /// <param name="tableName">The table from which to delete.</param>
        /// <param name="where">The where clause for the delete.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Delete(String tableName, String where)
        {
            Boolean returnCode = true;
            try
            {
                this.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));
            }
            catch (Exception fail)
            {
                MessageBox.Show(fail.Message);
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily insert into the DB
        /// </summary>
        /// <param name="tableName">The table into which we insert the data.</param>
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Insert(String tableName, Dictionary<String, String> data)
        {
            String columns = "";
            String values = "";
            Boolean returnCode = true;
            foreach (KeyValuePair<String, String> val in data)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                values += String.Format(" '{0}',", val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", tableName, columns, values));
            }
            catch (Exception fail)
            {
                MessageBox.Show(fail.Message);
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily delete all data from the DB.
        /// </summary>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearDB()
        {
            DataTable dtTables;
            try
            {
                dtTables = this.GetDataTable("select NAME from SQLITE_MASTER where type='table' order by NAME;");
                foreach (DataRow table in dtTables.Rows)
                {
                    this.ClearTable(table["NAME"].ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Allows the user to easily clear all data from a specific table.
        /// </summary>
        /// <param name="table">The name of the table to clear.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearTable(String table)
        {
            try
            {

                this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected void DoesTableExists(String table) {
            string name = this.ExecuteScalar(
                string.Format("SELECT name FROM sqlite_master WHERE type='table' AND name='{0}';", table)
            );
            if(name == null) {
                throw new Exception("Table " + table + " does not exists");
            }
        }
    }


    //public class ConnectionOpenedEventArgs : EventArgs {
    //    public TemplatesStorage Storage
    //    {
    //        get;
    //        protected set;
    //    }
    //
    //    public ConnectionOpenedEventArgs(string filename)
    //    {
    //        Storage = stor;
    //    }
    //}
    //
    //public class ConnectionClosedEventArgs : ConnectionOpenedEventArgs{ }
}
