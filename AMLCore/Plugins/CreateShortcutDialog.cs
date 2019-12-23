using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Plugins
{
    partial class CreateShortcutDialog : Form
    {
        public CreateShortcutDialog()
        {
            InitializeComponent();
        }

        public Internal.ShortcutStartupMode Mode { get; set; }
        public string FileName { get; set; }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.Text.Length != 0;
            FileName = textBox1.Text;
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                Mode = Internal.ShortcutStartupMode.Game;
            }
            else if (radioButton2.Checked)
            {
                Mode = Internal.ShortcutStartupMode.GSO;
            }
            else
            {
                Mode = Internal.ShortcutStartupMode.Launcher;
            }
        }
    }
}
