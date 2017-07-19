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
    
    internal partial class LauncherOptionForm : Form
    {
        public LauncherOptionForm(PluginContainer[] plugins, bool allowLoad)
        {
            InitializeComponent();
            Options = plugins;
            foreach (var p in plugins)
            {
                listView1.Items.Add(new ListViewItem(p.AssemblyName)
                {
                    Tag = p,
                });
            }
            button4.Enabled = allowLoad;
        }

        public PluginContainer[] Options { get; private set; }
        public LaunchMode LauncherMode { get; private set; }

        private PluginContainer[] GetOptions()
        {
            return listView1.Items.OfType<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => (PluginContainer)i.Tag)
                .ToArray();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Options = GetOptions();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.LauncherMode = LaunchMode.NewGame;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Options = GetOptions();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.LauncherMode = LaunchMode.NewOnline;
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Options = GetOptions();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.LauncherMode = LaunchMode.InjectOnline;
            this.Close();
        }
    }
}
