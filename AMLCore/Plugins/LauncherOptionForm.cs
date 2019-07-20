using AMLCore.Internal;
using AMLCore.Misc;
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
                listView1.Items.Add(new ListViewItem(p.DisplayName)
                {
                    Tag = p,
                });
                var ctrl = p.GetConfigControl();
                if (ctrl != null)
                {
                    var page = new TabPage(p.DisplayName);
                    page.Controls.Add(ctrl);
                    ctrl.Dock = DockStyle.Fill;
                    tabControl1.TabPages.Add(page);
                    _EditControls.Add(ctrl);
                }
            }
            button4.Enabled = allowLoad;
            
            this.InitPresetList();

            DisableUnfinished();
            this.tabControl1.SelectTab(0); //TODO
        }

        private void DisableUnfinished()
        {
            //button3.Enabled = false;
            button4.Enabled = false;
            tabControl1.TabPages.RemoveAt(1);
            tabControl1.TabPages.RemoveAt(0);
        }

        public PluginContainer[] Options { get; private set; }
        public LaunchMode LauncherMode { get; private set; }
        private List<Control> _EditControls = new List<Control>();

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

        private void button5_Click(object sender, EventArgs e)
        {
            if (OnlineUpdateCheck.CheckOnly())
            {
                label2.Text = OnlineUpdateCheck.LatestVersion;
                label1.Visible = true;
                label2.Visible = true;
                button6.Visible = true;
            }
            else
            {
                label2.Text = OnlineUpdateCheck.LatestVersion;
                label1.Visible = true;
                label2.Visible = true;
                button6.Visible = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OnlineUpdateCheck.Check();
        }

        #region preset

        private class Preset : CommonArguments
        {
            public string Name;

            public Preset(string name)
            {
                Name = name;
            }
            public Preset(string name, IEnumerable<Preset> p) : base(p)
            {
                Name = name;
            }
        }

        private List<Preset> _Presets = new List<Preset>();

        private void InitPresetList()
        {
            ReadPresets();
            foreach (var p in _Presets)
            {
                listView2.Items.Add(new ListViewItem(p.Name) { Tag = p });
            }
            listView2.Items[0].Selected = true;
            listView2.Items[0].Checked = true;
            SwitchEditMode(true, 0);
        }

        private void ReadPresets()
        {
            _Presets.Add(new Preset("(默认)"));
        }

        private void SavePresets()
        {
        }

        private void LoadPreset(Preset p)
        {
            p.SetPluginOptions(Options);
            var mods = p.SplitMods();
            foreach (var item in listView1.Items.OfType<ListViewItem>())
            {
                item.Checked = mods.Contains(((PluginContainer)item.Tag).AssemblyName);
            }
        }

        private void LoadPresets()
        {
            LoadPreset(new Preset(null,
                GetCheckedPresetIds().Select(id => _Presets[id]).Reverse()));
        }

        private void FinishEditPreset(Preset p)
        {
            p.GetPluginOptions(Options);
        }

        private int GetCurrentSelectedPresetId()
        {
            return listView2.SelectedIndices.Count == 1 ?
                listView2.SelectedIndices[0] : -1;
        }

        private int[] GetCheckedPresetIds()
        {
            return listView2.Items.OfType<ListViewItem>()
                .Where(t => t.Checked).Select(t => t.Index).ToArray();
        }

        private void SwitchEditMode(bool editPreset, int edit)
        {
            if (editPreset)
            {
                button7.Enabled = true;
                button12.Enabled = false;
                listView2.Enabled = false;
                foreach (var ctrl in _EditControls)
                {
                    ctrl.Enabled = true;
                }
                listView1.Enabled = true;
                LoadPreset(_Presets[edit]);
            }
            else
            {
                button7.Enabled = false;
                button12.Enabled = true;
                listView2.Enabled = true;
                foreach (var ctrl in _EditControls)
                {
                    ctrl.Enabled = false;
                }
                listView1.Enabled = false;
                LoadPresets();
            }
        }

        private int _Editing = 0; //initially on default
        private void button12_Click(object sender, EventArgs e)
        {
            var id = GetCurrentSelectedPresetId();
            if (id == -1)
            {
                return;
            }
            _Editing = id;
            SwitchEditMode(true, id);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (_Editing != -1)
            {
                _Presets[_Editing].GetPluginOptions(Options);
                _Presets[_Editing].Mods = String.Join(",",
                    listView1.Items.OfType<ListViewItem>()
                    .Where(i => i.Checked)
                    .Select(i => ((PluginContainer)i.Tag).AssemblyName));
            }
            SwitchEditMode(false, -1);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string n;
            for (int i = 1; ; ++i)
            {
                n = "Preset " + i.ToString();
                if (!_Presets.Any(pp => pp.Name == n))
                {
                    break;
                }
            }
            var p = new Preset(n);
            _Presets.Add(p);
            listView2.Items.Add(new ListViewItem(n) { Tag = p });
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var id = GetCurrentSelectedPresetId();
            if (id == -1)
            {
                return;
            }
            _Presets.RemoveAt(id);
            listView2.Items.RemoveAt(id);
        }

        private void listView2_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            LoadPresets();
        }

        private void listView2_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.Item.Index == 0)
            {
                textBox1.Enabled = false;
                textBox1.Text = "";
                button9.Enabled = false;
                button10.Enabled = false;
                button11.Enabled = false;
            }
            else
            {
                textBox1.Enabled = true;
                textBox1.Text = e.Item.Text;
                button9.Enabled = true;
                button10.Enabled = true;
                button11.Enabled = true;
            }
            if (e.Item.Index == 1)
            {
                button10.Enabled = false;
            }
            if (e.Item.Index == listView2.Items.Count - 1)
            {
                button11.Enabled = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var id = GetCurrentSelectedPresetId();
            if (id == -1 && id != 0)
            {
                return;
            }
            _Presets[id].Name = textBox1.Text;
            listView2.Items[id].Text = textBox1.Text;
        }

        #endregion
    }
}
