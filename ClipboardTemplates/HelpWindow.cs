﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipboardTemplates
{
    public partial class HelpWindow : Form
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void HelpWindow_Load(object sender, EventArgs e)
        {
            HtmlDocument doc = this.browser.Document;
            doc.Write(Properties.Resources.helpPage);

            Properties.Settings.Default.ShowHelpAfterStart = false;
            Console.Write(Properties.Settings.Default.ShowHelpAfterStart);
        }
    }
}
