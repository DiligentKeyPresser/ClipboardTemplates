using ClipboardTemplates.models;
using ClipboardTemplates.models.TemplatesDataSetTableAdapters;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Windows.Forms;


// New things for me:

// new in C#2

// nullable variables:
// Nullable<int> a = null;
// int? a = null; a.HasValue = true|false; a.Value;

// coalescing operator (return first non-null displayField)
// int b  = 5
// int c  = a ?? b;

// Fast tracked delegates
// - 



namespace ClipboardTemplates
{

    // This is my main class
    // It is instanciated in program.cs
    public partial class MainWindow : Form
    {

        // internals
        protected KeyboardHook hook = new KeyboardHook();
        protected ClipboardTemplates.models.TemplatesDatabase database;

        /// <summary>
        /// Shortcut for this.database.DataSet
        /// </summary>
        protected TemplatesDataSet dataSet
        {
            get { return this.database.DataSet; }
        }

        /// <summary>
        /// Shortcut for this.database.TableAdapterManager.TemplatesTableAdapter
        /// </summary>
        protected TemplatesTableAdapter dataAdapter
        {
            get { return this.database.TableAdapterManager.TemplatesTableAdapter; }
        }

        protected string status
        {
            set { this.toolStripStatusLabel.Text = value; }
        }

        // settings
        internal ClipboardTemplates.Properties.Settings settings;

        // Constructor
        public MainWindow()
        {
            // Initialize componenents from designer
            InitializeComponent();

            // Load settings (using settings generated and stored by class from Visual Studio)
            // also see method (saving settings there)
            // @see MainWindow_FormClosed
            this.settings = this.getSettings();
        }

        /// <summary>
        /// Once window is rendered...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            // Show dialog with help after program start
            if (this.settings.ShowHelpAfterStart)
            {
                (new HelpWindow()).ShowDialog();
                this.settings.ShowHelpAfterStart = false;
            }

            // (sync windows state)
            // update message in statusbar, etc...
            this.database_onConnectionClosed(); 

            // this brackets means block of code; so local variable will be discarted when we go out this code block
            {
                // load string where DB is from settings
                try
                {
                    // if DatabasePath is "", it will only update statusbar
                    this.OpenDatabaseFile(this.settings.DatabasePath);
                }
                catch (Exception ex)
                {
                    this.showException("Can't open database!", ex);
                }

                // this will update database; so it should be connected to databse
                // if not, something went wrong; so let user know, that no database was loaded and how he can load one
                if (this.database == null)
                {
                    this.showMessage(Properties.Resources.Msg_SetupDatabase);
                }
            }

            
            this.initHotkeys(); // from loaded datasource
        }

        /// <summary>
        /// Called when form is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.saveSettings();
        }




        
        #region Model and database management

        protected void buildDatabaseObject(string fName, bool setupDB = false)
        {
            var m = this.database = new TemplatesDatabase(fName, setupDB);
            m.ConnectionClosed += delegate { this.database_onConnectionClosed(); };
            m.ChangesSaved += delegate { this.database_onChangesSaved(); };
            database_onConnectionOpened(); // call internal "event"
        }

        void database_onConnectionClosed()
        {
            this.status = Properties.Resources.Msg_NoDatabaseOpenned;

            this.templatesToolStripMenuItem_Templates.Enabled = false;
            foreach (ToolStripMenuItem c in this.templatesToolStripMenuItem_Templates.DropDownItems)
            {
                c.Enabled = false;
            }
            this.contextMenuStripGridRightClick.Enabled = false;
            this.releaseToolStripMenuItem.Enabled = false;

            this.PopulateGrid();
        } 
        
        void database_onConnectionOpened()
        {
            this.status = string.Format(Properties.Resources.Msg_WhichDatabaseOpened, this.database.DatabaseName);

            this.templatesToolStripMenuItem_Templates.Enabled = true;
            foreach(ToolStripMenuItem c in this.templatesToolStripMenuItem_Templates.DropDownItems) {
                c.Enabled = true;
            }
            this.contextMenuStripGridRightClick.Enabled = true;
            this.releaseToolStripMenuItem.Enabled = true;
            
            // Refresh grid
            this.PopulateGrid(true);
        }

        void database_onChangesSaved()
        {
            // Yes, really want to load it again from database
            // There will be new primary key - otherwise first modification -> exception
            this.PopulateGrid(true);
        }

        protected void databaseSaveChangesOrShowErrorMessage()
        {
            try
            {
                this.database.SaveChanges();
            }
            catch (Exception e)
            {
                this.showException("Error while saving changes!", e);
            }
        }
        #endregion






        #region Grid: Add, edit, remove row handlers + pupulateGrid() + grid event handlers
        private void PopulateGrid(bool askDB = false)
        {
            if(askDB) this.dataAdapter.Fill(this.dataSet.Templates);

            // Unregister all shortcuts
            this.hook.UnregisterAll();
            this.listView.Items.Clear();

            // Empty dataset, user wanted just to clear the grid
            if (this.database == null) return;

            this.listView.BeginUpdate();
            foreach (TemplatesDataSet.TemplatesRow dr in this.database.DataSet.Templates.Select())
            {
                var li = new ListViewItem();
                li.Tag = dr.IdTemplate;
                li.Text = dr.Name;
                li.SubItems.Add(this.getShortcutAsAString(dr));
                li.SubItems.Add(dr.Description);
                this.listView.Items.Add(li);
            }
            this.listView.EndUpdate();

            this.registerShortcuts(this.database.DataSet.Templates);
        }

        /// <summary>
        /// Getts shortcut from row as a string
        /// TODO: Move to that row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected string getShortcutAsAString(TemplatesDataSet.TemplatesRow row)
        {
            
            if (row.IsShortcut_KeyNull())
            {
                return "";
            }
            long key = row.Shortcut_Key;
            string shortcut_s = "";
            if (row.Shortcut_Ctrl) shortcut_s += "Ctrl + ";
            if (row.Shortcut_Alt) shortcut_s += "Alt + ";
            if (row.Shortcut_Shift) shortcut_s += "Shift + ";
            if (row.Shortcut_Win) shortcut_s += "Win + ";

            shortcut_s += keyCode2Name((Keys) key);

            return shortcut_s;
        }

        public string keyCode2Name(Keys k)
        {
            var t = typeof(Keys);
            return (string)Enum.GetName(t, k); ;
        }

        //public Keys keyName2Code(string name)
        //{
        //
        //}



        protected void gridEditActiveItem()
        {
            // Get listviewitem (used in list view), than search by primary valueField (saved in Tag property)
            ListViewItem currentListItem = listView.FocusedItem;
            if (currentListItem == null) return;

            // find record we want to modify
            TemplatesDataSet.TemplatesRow[] DataRowArray = 
                (TemplatesDataSet.TemplatesRow[])
                this.dataSet.Templates.Select(
                    string.Format("IdTemplate = {0}", currentListItem.Tag)
                );

            // shoud be only one, we are filtering using primary valueField
            if (DataRowArray.Length != 1)
            {
                this.showMessage(Properties.Resources.Msg_CannotRetrieveRowFromDataCollection);
                return;
            }

            // Open editor, and if any changes preformed notify database
            if (this.editTemplate_dataRow(DataRowArray[0]))
            {
                try
                {
                    this.databaseSaveChangesOrShowErrorMessage();
                }
                catch (System.Data.SQLite.SQLiteException e)
                {
                    if (e.ErrorCode == 19)
                    {
                        // TODO: popup editing window again with unchanges values!
                        showMessage(Properties.Resources.Msg_DuplicatedShortcut);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected bool editTemplate_dataRow(TemplatesDataSet.TemplatesRow r)
        {
            var w = new TemplateEditor(r);
            var res = w.ShowDialog();
            w.Dispose();
            if (res != DialogResult.OK) return false;
            return r.RowState != DataRowState.Unchanged;
        }

        private void gridAddNewItem()
        {
            /// @link http://msdn.microsoft.com/en-us/library/5ycd1034(v=vs.80).aspx
            TemplatesDataSet.TemplatesRow r = (TemplatesDataSet.TemplatesRow) this.dataSet.Templates.NewRow();

            if (this.editTemplate_dataRow(r))
            {
                try
                {
                    this.dataSet.Templates.Rows.Add(r);
                    this.databaseSaveChangesOrShowErrorMessage();
                }
                catch (Exception e)
                {
                    this.showException("Your data aren't valid!", e);
                }
            }
        }


        private void gridDeleteActiveItem()
        {
            

            // Get listviewitem (used in list view), than search by primary valueField (saved in Tag property)
            ListViewItem currentListItem = listView.FocusedItem;
            if (currentListItem == null) return;

            // find record we want to modify
            TemplatesDataSet.TemplatesRow[] DataRowArray = 
                (TemplatesDataSet.TemplatesRow[])
                this.dataSet.Templates.Select(
                    string.Format("IdTemplate = {0}", currentListItem.Tag)
                );
            if (DataRowArray.Length != 1) // shoud be only one, we are filtering using primary valueField
            {
                this.showMessage(Properties.Resources.Msg_CannotRetrieveRowFromDataCollection);
                return;
            }

            var row = DataRowArray[0];

            if (MessageBox.Show("Are you sure that you want to delete '" + row.Name + "' item?", "ClipboardTemplates", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                row.Delete();
                this.databaseSaveChangesOrShowErrorMessage();
            }
        }


        // ------------------ GRID EVENT HANDLERS -----------------------------------

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.gridEditActiveItem();
        }

        private void listView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) this.gridEditActiveItem();
        }

        #region Grid: context menu events
        private void addNewShortcutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.gridAddNewItem();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.gridEditActiveItem();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.gridDeleteActiveItem();
        }
        #endregion


        #endregion




        /// <summary>
        /// Getts application settings
        /// </summary>
        /// <returns></returns>
        internal ClipboardTemplates.Properties.Settings getSettings()
        {
            return Properties.Settings.Default;
        }

        protected void saveSettings()
        {
            this.settings.Save();
        }















        #region Database: Open and create new dialog
        /// <summary>
        /// Opens filepicker and continues with process of creating new database
        /// - also notifies database and updates status bar in this form
        /// </summary>
        /// <returns></returns>
        public bool CreateNewDatabase()
        {
            DialogResult r = this.saveFileDialog.ShowDialog();
            if (r != DialogResult.OK) return false;

            string fPath = this.saveFileDialog.FileName;

            try
            {
                this.CloseDatabaseFile();

                // Update application database instance
                OpenDatabaseFile(fPath, true);
            }
            catch (Exception e)
            {
                this.showException(Properties.Resources.Msg_CannotCreateDBFile, e);
                return false;
            }

            return true;
        }



        

        /// <summary>
        /// Closes database file (notifies database) and updates status bar
        /// </summary>
        protected void CloseDatabaseFile()
        {
            if (this.database == null) return;
            var db = this.database;
            this.database = null;
            db.CloseConnection();
            this.settings.DatabasePath = "";
        }


        public void ReleaseDatabaseFile()
        {
            this.CloseDatabaseFile();
        }


        /// <summary>
        /// Opens filepicker to allow user to open existing database
        /// this fn also notifies mode and UI in this form (indirectly updates status bar)
        /// </summary>
        /// <returns></returns>
        public bool OpenDatabaseFile()
        {
            var r = this.openFileDialog.ShowDialog();
            if (r == DialogResult.Cancel) return false;
            String fPath = this.openFileDialog.FileName;

            try
            {
                this.OpenDatabaseFile(fPath);
            }
            catch (Exception e)
            {
                this.showException(Properties.Resources.Msg_CannotCreateDBFile, e);
                return false;
            }
            return true;
        }
        public void OpenDatabaseFile(string path, Boolean openAsNew = false)
        {
            this.CloseDatabaseFile();
            if (path == "")
                return;

            try
            {
                this.buildDatabaseObject(path, openAsNew); // updates this.database
                this.settings.DatabasePath = path;
            }
            catch (Exception e)
            {
                throw e;
                return;
            }
        }  
        #endregion




        

        #region Hotkeys management: move to separate class

        /// <summary>
        /// Registers shortcuts that are described in given data table
        /// </summary>
        /// <param name="t"></param>
        protected void registerShortcuts(TemplatesDataSet.TemplatesDataTable t) { 
            var h = this.hook;
            ClipboardTemplates.ModifierKeys m = 0;
            Keys k = 0;

            foreach (var r in t)
            {
                if (r.IsShortcut_KeyNull()) continue;
                long ki = r.Shortcut_Key; 

                if (r.Shortcut_Ctrl) m |= ClipboardTemplates.ModifierKeys.Control;
                if (r.Shortcut_Alt) m |= ClipboardTemplates.ModifierKeys.Alt;
                if (r.Shortcut_Shift) m |= ClipboardTemplates.ModifierKeys.Shift;
                if (r.Shortcut_Win) m |= ClipboardTemplates.ModifierKeys.Win;
                k = (Keys) ki;
                try
                {
                    var itm = this.hook.RegisterHotKey(m, k);
                    itm.CustomData = r;
                }
                catch (InvalidOperationException)
                {
                    this.showMessage("There was a problem with registering "+getShortcutAsAString(r)+"!");
                }
            }
        }

        /// <summary>
        /// Call this method on initialize
        /// </summary>
        private void initHotkeys()
        {
            this.hook.KeyPressed += onShortcutFired;
        }

        protected bool CtrlVLock = false;

        protected void onShortcutFired(object sender, KeyPressedEventArgs args)
        {
            if (this.CtrlVLock) { return; }

            KeyboardHook_item khi = (KeyboardHook_item) sender;
            TemplatesDataSet.TemplatesRow dr = (TemplatesDataSet.TemplatesRow) khi.CustomData;
            if (dr == null) return;

            
            string puvodniClipboard = Clipboard.GetText(); // Zaloha obsahu clipboard

            // Copy template to clipboard
            string template = dr.Template.Replace("\r\n", "\n"); // Mac, Unix newlines to Win newlines

            // Using application direct write
            Thread.Sleep(new TimeSpan(0, 0, 0, 0, 500));

            // Overring clipboard with current selection, if template wants to do that
            if (template.Contains("<copy!/>\n"))
            {
                SendKeys.SendWait("^(C)");  // Send CTRL+C
                SendKeys.Flush();
                template = template.Replace("<copy!/>\n", "");
            }

            // Macros
            template = template.Replace("<clipboard/>", Clipboard.GetText());

            this.CtrlVLock = true;
            Clipboard.SetText(template);
            SendKeys.SendWait("+{INSERT}");
            SendKeys.Flush();
            try
            {
                Clipboard.SetText(puvodniClipboard); // obnovime puvodni obsah clipboardu
            }
            catch (System.Runtime.InteropServices.ExternalException e)
            {/*do nothing*/}
            this.CtrlVLock = false;
            
            // escape string
            // http://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys.send.aspx
            // template = template.Replace("{", "----{{----")
            //     .Replace("}", "----}}----")
            //     .Replace("----{{----", "{{}")
            //     .Replace("----}}----", "{}}")
            //     .Replace("+", "{+}")
            //     .Replace("^", "{^}")
            //     .Replace("%", "{%}")
            //     .Replace("~", "{~}")
            //     .Replace("(", "{(}")
            //     .Replace(")", "{)}")
            //     .Replace("[", "{[}")
            //     .Replace("]", "{]}")
            // 
            //     .Replace("\n", "{ENTER}")
            //     .Replace("\r", "{ENTER}");

            // Send result to application
            // SendKeys.SendWait(template);
            // SendKeys.Flush();



            // Notify, that all has been done
            //this.notifyIcon.ShowBalloonTip(1, "ClipboardTemplates", "Template has been sent to application.", ToolTipIcon.Info);


            // Copy to clipboard
            //var originalClipboardContent = Clipboard.GetDataObject();
            //Clipboard.SetText(dr.Template);
            //
            //this.notifyIcon.ShowBalloonTip(1, "ClipboardTemplate", "Your template has been copied to clipboard", ToolTipIcon.Info);
            //
            ////Thread.Sleep(new TimeSpan(0,0,0,0,250));
            //
            ////SendKeys.SendWait("^V");
            ////SendKeys.SendWait("+{INSERT}");
            //
            //Clipboard.SetDataObject(originalClipboardContent);
        }

        #endregion













        #region Minimize to try (overriding Close method)
        /// <summary>
        /// Shutdowns the program (closes this form)
        /// </summary>
        public void ShutdownProgram()
        {
            this.Close(true); // force closing, otherwise it will try to minimize to try
        }


        // Close == hide window == minimize to tray (if closeFormIsHideForm == true && this.database.Connected)
        protected Boolean closeFormIsHideForm = true;

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!closeFormIsHideForm || this.database == null) return;

            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        public void Close(Boolean force)
        {
            if (force)
            {
                closeFormIsHideForm = false;
            }
            this.Close();
        } 
        #endregion



        #region Menu strip events
        // Quit in program menu
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShutdownProgram();
        }

        // Hide program from program menu
        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        // Database -> New
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.CreateNewDatabase();
        }

        // Database -> Open
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.OpenDatabaseFile();
        }

        // Databse -> Release
        private void releaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ReleaseDatabaseFile();
        }

        private void MainWindow_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (new HelpWindow()).Show();
        }

        private void aboutProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new AboutBox()).ShowDialog();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new HelpWindow()).ShowDialog();
        }
        #endregion



        #region Notification icon events
        // Show settings
        private void showToolStripMenuItem_NotifIcon_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Focus();
        }

        // Close program
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShutdownProgram();
        }

        // Clicked on notif icon
        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Show();
                this.Focus();
            }
        } 
        #endregion

        
        #region Helpers: Showing messages (show*() methods)

        protected void showMessage(string text)
        {
            this.showMessage(text, MessageBoxIcon.Information);
        }
        protected void showMessage(string text, MessageBoxIcon icon)
        {
            MessageBox.Show(
                text,
                "ClipboardTemplates",
                MessageBoxButtons.OK,
                icon
            );
        }
        protected void showException(string text, Exception e)
        {
            this.showMessage(
                text + "\n\n" + Properties.Resources.Msg_MoreInfo + ": " + e.Message,
                MessageBoxIcon.Error
            );
        }

        #endregion








    }
}
