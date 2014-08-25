using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace ClipboardTemplates
{
    public partial class TemplateEditor : Form
    {
        protected DataRow row;
        public DataRow Row
        {
            get { return this.row; }
            set { this.loadDataRow(value); }
        }

        public TemplateEditor(DataRow r)
        {
            InitializeComponent();

            fillComboBoxWithKeys(this.fieldKey);

            this.Icon = Properties.Resources.icoLightning;

            this.Row = r;
        }


        protected void populateForm()
        {
            this.loadDataRow(this.row);
        }


        /// <summary>
        /// Loads data from record into form
        /// </summary>
        /// <param name="r"></param>
        protected void loadDataRow(DataRow r) {
            if (r == null) { this.row = null; return; }
            this.row = r;
            if (r.RowState == DataRowState.Detached) return; // Noting to load

            long id = (long)r["IdTemplate"];
            this.fieldName.Text        = (string)r["Name"];
            this.fieldDescription.Text = (string)r["Description"];
            this.fieldTemplate.Text    = (string)r["Template"];

            this.fieldCtrl.Checked = (bool)r["Shortcut_Ctrl"];
            this.fieldAlt.Checked = (bool)r["Shortcut_Alt"];
            this.fieldShift.Checked = (bool)r["Shortcut_Shift"];
            this.fieldWin.Checked = (bool)r["Shortcut_Win"];
            var key = r.Field<Int64?>("Shortcut_Key");
            this.fieldKey.SelectedItem = this.keyCodeToSelectItem(key == null ? 0 : key.Value);

            //this.row.AcceptChanges();
        }

        protected ComboKeyItem keyCodeToSelectItem(Int64 code) {
            foreach (ComboKeyItem i in this.fieldKey.Items) {
                if (i.valueField == code) return i;
            }
            return null;
        }




        /// <summary>
        /// This updates given DataRow according to data in form
        /// </summary>
        /// <param name="r"></param>
        protected void updateDataRow(DataRow r) {
            r.BeginEdit();
            r["Name"] = this.fieldName.Text;
            r["Description"] = this.fieldDescription.Text;
            r["Template"] = this.fieldTemplate.Text;

            r["Shortcut_Ctrl"] = this.fieldCtrl.Checked;
            r["Shortcut_Alt"] = this.fieldAlt.Checked;
            r["Shortcut_Shift"] = this.fieldShift.Checked;
            r["Shortcut_Win"] = this.fieldWin.Checked;

            Int64 key;
            if (
                this.fieldKey.SelectedItem == null ||
                (key = ((ComboKeyItem)this.fieldKey.SelectedItem).valueField) == 0
            ) {
                r["Shortcut_Key"] = DBNull.Value;
            }
            else
            {
                r["Shortcut_Key"] = key;
            }
            r.EndEdit();
        }

        //private int? convertKeyNameToKeyCode(string keyString)
        //{
        //    if (string.IsNullOrEmpty(keyString)) return null;

        //    // using reflection to access property values of enum named Keys
        //    var tKeys = typeof(Keys);
        //    Keys k = (Keys)Enum.Parse(tKeys, keyString);
        //    if (!Enum.IsDefined(tKeys, k))
        //    {
        //        throw new Exception("Key selected in combobox not recognized!");
        //    }

        //    return k.GetHashCode();
        //}


        // UI events:
        private void winOkBtn_Click(object sender, EventArgs e)
        {
            this.updateDataRow(this.row);
            this.Close();
        }

        private void winCancelBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void fillComboBoxWithKeys(ComboBox combo)
        {
            List<ComboKeyItem> comboList = new List<ComboKeyItem>();

            // Get all possible keys
            var tKeys = typeof(Keys);
            foreach (var val in Enum.GetValues(tKeys))
            {
                var name = Enum.GetName(tKeys, val);

                var item = new ComboKeyItem();
                item.displayField = name;
                item.valueField = (int)val;
                comboList.Add(item);
            }

            combo.DisplayMember = "displayField";
            combo.ValueMember = "valueField";
            combo.DataSource = comboList;
        }

        protected class ComboKeyItem
        {
            public string displayField;
            public Int64 valueField;

            public override string ToString()
            {
                return displayField;
            }
        }

    }
}

