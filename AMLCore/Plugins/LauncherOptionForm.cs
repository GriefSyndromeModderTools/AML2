﻿using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
                    page.Tag = p;
                    ctrl.Dock = DockStyle.Fill;
                    tabControl1.TabPages.Add(page);
                    _EditControls.Add(ctrl);
                }
            }
            button4.Enabled = allowLoad && false;
            
            this.InitPresetList();

            DisableUnfinished();
            this.tabControl1.SelectTab(0); //TODO

            PrepareDisableListViewItem(listView1);
            PrepareDisableListViewItem(listView2);
        }

        public bool SelectLauncherMode
        {
            get => button1.Visible;
            set
            {
                button1.Visible = value;
                button3.Visible = value;
                button4.Visible = value;
                button11.Visible = !value;
            }
        }

        public void DisablePresetEdit()
        {
            button7.Enabled = false;
            button12.Enabled = false;
        }

        public void DisableNonFunctional()
        {
            HashSet<string> excludedItems = new HashSet<string>();
            foreach (var m in Options)
            {
                if (m.Type != PluginType.Functional)
                {
                    excludedItems.Add(m.InternalName);
                }
            }
            DisableItems(excludedItems);
        }

        public void DisableGSO()
        {
            HashSet<string> excludedItems = new HashSet<string>();
            foreach (var m in Options)
            {
                if (m.HasGSOLoaded)
                {
                    excludedItems.Add(m.InternalName);
                }
            }
            DisableItems(excludedItems);
        }

        private void DisableItems(HashSet<string> excludedItems)
        {
            foreach (var modItem in listView1.Items.OfType<ListViewItem>())
            {
                var m = (PluginContainer)modItem.Tag;
                if (excludedItems.Contains(m.InternalName))
                {
                    DisableListViewItem(modItem);
                }
            }
            foreach (var presetItem in listView2.Items.OfType<ListViewItem>())
            {
                var p = (Preset)presetItem.Tag;
                if (p == _Presets[0])
                {
                    continue;
                }
                foreach (var m in p.SplitMods())
                {
                    if (excludedItems.Contains(m))
                    {
                        DisableListViewItem(presetItem);
                        break;
                    }
                }
            }
            foreach (var tab in tabControl1.TabPages.OfType<TabPage>())
            {
                var m = (PluginContainer)tab.Tag;
                if (m != null && excludedItems.Contains(m.InternalName))
                {
                    foreach (var ctrl in tab.Controls.OfType<Control>())
                    {
                        ctrl.Enabled = false;
                    }
                }
            }
        }

        private static void PrepareDisableListViewItem(ListView listView)
        {
            listView.Tag = new HashSet<object>();
            listView.ItemSelectionChanged += (sender, e) =>
            {
                var item = e.Item;
                if (item.ForeColor != Color.Gray)
                {
                    return;
                }
                if (e.Item.Selected)
                {
                    e.Item.Selected = false;
                }
            };
            listView.ItemCheck += (sender, e) =>
            {
                var item = ((ListView)sender).Items[e.Index];
                if (item.ForeColor != Color.Gray)
                {
                    return;
                }
                var shouldBeChecked = ((HashSet<object>)((ListView)sender).Tag).Contains(item.Tag);
                if (shouldBeChecked != (e.NewValue == CheckState.Checked))
                {
                    e.NewValue = shouldBeChecked ? CheckState.Checked : CheckState.Unchecked;
                    //item.Checked = shouldBeChecked;
                }
            };
        }

        private static void DisableListViewItem(ListViewItem listViewItem)
        {
            if (listViewItem.ForeColor == Color.Gray)
            {
                return;
            }
            listViewItem.ForeColor = Color.Gray;
            if (listViewItem.Checked)
            {
                ((HashSet<object>)listViewItem.ListView.Tag).Add(listViewItem.Tag);
            }
        }

        private void DisableUnfinished()
        {
            //button3.Enabled = false;
            button4.Enabled = false;
            //tabControl1.TabPages.RemoveAt(1);
            tabControl1.TabPages.RemoveAt(0);
        }

        public void LoadArgPresetOptions(string presetOptions)
        {
            if (presetOptions == null) return;
            var seg = presetOptions.Split(';');
            if (seg.Length < 2) throw new Exception("Invalid preset options");

            var presets = seg[0].Split(',');
            for (int i = 0; i < _Presets.Count; ++i)
            {
                if (presets.Contains(_Presets[i].Name))
                {
                    listView2.Items[i].Checked = true;
                }
            }

            seg[1] = "Mods=" + seg[1];
            _Presets[0].ParseModsAndOptions(seg.Skip(1).ToArray());

            RefreshControls();
        }

        public void LoadArgPresetOptions(PresetSelection presetSelection)
        {
            if (presetSelection == null) return;
            var presets = new HashSet<string>(presetSelection.SelectedPresets.Select(p => p.Preset));

            //Start from 1.
            for (int i = 1; i < _Presets.Count; ++i)
            {
                if (presets.Contains(_Presets[i].Name))
                {
                    listView2.Items[i].Checked = true;
                }
            }

            _Presets[0].Mods = presetSelection.DefaultPreset.Mods;
            _Presets[0].Options.Clear();
            _Presets[0].Options.AddRange(presetSelection.DefaultPreset.Options);

            RefreshControls();
        }

        public PluginContainer[] Options { get; private set; }

        public LaunchMode LauncherMode { get; private set; }
        private List<Control> _EditControls = new List<Control>();

        private PluginContainer[] GetOptions()
        {
            if (_Editing != -1)
            {
                button7_Click(button7, EventArgs.Empty);
            }

            return listView1.Items.OfType<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => (PluginContainer)i.Tag)
                .ToArray();
        }

        public PresetSelection GetPresetSelection()
        {
            return new PresetSelection(_Presets[0],
                GetCheckedPresetIds().Where(i => i != 0).Select(i => _Presets[i]));
        }

        private ShortcutArguments GetShortcutOptions(Internal.ShortcutStartupMode mode)
        {
            int e = _Editing;
            if (e != -1)
            {
                button7_Click(button7, EventArgs.Empty);
            }

            var ret = ShortcutArguments.Create(listView1.Items.OfType<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => (PluginContainer)i.Tag)
                .ToArray(), mode);

            if (e != -1)
            {
                _Editing = e;
                RefreshControls();
            }

            return ret;
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

        private void button11_Click(object sender, EventArgs e)
        {
            this.Options = GetOptions();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.LauncherMode = LaunchMode.NewGame;
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

        private List<Preset> _Presets = new List<Preset>();
        private int _Editing = 0; //initially on default

        private void InitPresetList()
        {
            ReadPresets();
            foreach (var p in _Presets)
            {
                listView2.Items.Add(new ListViewItem(p.Name) { Tag = p });
            }
            listView2.Items[0].Selected = true;
            listView2.Items[0].Checked = true;
            //SwitchEditMode(true, 0);
            RefreshControls();
        }

        private void ReadPresets()
        {
            _Presets.Add(Preset.CreateDefaultPreset());
            _Presets[0].Mods = string.Join(",",
                Options.Where(oo => oo.Type == PluginType.Optimization).Select(oo => oo.AssemblyName));

            foreach (var c in Options)
            {
                c.CollectPresets(_Presets);
            }

            Preset.GetPresetsFromJson(_Presets, false);
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
            foreach (var ctrl in _EditControls)
            {
                ctrl.Refresh();
            }
        }

        private void LoadPresets()
        {
            LoadPreset(new Preset(GetCheckedPresetIds().Select(id => _Presets[id]).Reverse()));
        }

        //private void FinishEditPreset(Preset p)
        //{
        //    p.GetPluginOptions(Options);
        //}

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

        private void RefreshControls()
        {
            if (_Editing == -1)
            {
                //Show combined preset, disable all editing functions
                LoadPresets();
                textBox1.Text = "";
                textBox1.Enabled = false;
                var sel = GetCurrentSelectedPresetId();
                button12.Enabled = sel != -1;
                button7.Enabled = false;
                foreach (var ctrl in _EditControls)
                {
                    ctrl.Enabled = false;
                }
                listView1.SelectedIndices.Clear();
                listView1.Enabled = false;

                //Allow adding new presets/removing presets
                button8.Enabled = true;
                button9.Enabled = sel > 0 && _Presets[sel].Editable;
            }
            else if (_Editing == 0)
            {
                //Can't edit name
                //Allow editing preset
                LoadPreset(_Presets[0]);

                textBox1.Text = "";
                textBox1.Enabled = false;
                button12.Enabled = false;
                button7.Enabled = true;
                foreach (var ctrl in _EditControls)
                {
                    ctrl.Enabled = true;
                }
                listView1.SelectedIndices.Clear();
                listView1.Enabled = true;

                button8.Enabled = false;
                button9.Enabled = false;
            }
            else if (_Presets[_Editing].Editable)
            {
                //Allow all editing
                LoadPreset(_Presets[_Editing]);

                textBox1.Text = _Presets[_Editing].Name;
                textBox1.Enabled = true;
                button12.Enabled = false;
                button7.Enabled = true;
                foreach (var ctrl in _EditControls)
                {
                    ctrl.Enabled = true;
                }
                listView1.SelectedIndices.Clear();
                listView1.Enabled = true;

                button8.Enabled = false;
                button9.Enabled = false;
            }
            else
            {
                //Disallow all editing
                LoadPreset(_Presets[_Editing]);

                textBox1.Text = _Presets[_Editing].Name;
                textBox1.Enabled = false;
                button12.Enabled = false;
                button7.Enabled = true;
                foreach (var ctrl in _EditControls)
                {
                    ctrl.Enabled = false;
                }
                listView1.SelectedIndices.Clear();
                listView1.Enabled = false;

                button8.Enabled = false;
                button9.Enabled = false;
            }

            for (int i = 0; i < _Presets.Count; ++i)
            {
                if (i == _Editing)
                {
                    listView2.Items[i].Text = _Presets[i].Name + " (正在编辑)";
                }
                else
                {
                    listView2.Items[i].Text = _Presets[i].Name;
                }
            }
        }

        //private void SwitchEditMode(bool editPreset, int edit)
        //{
        //    if (editPreset)
        //    {
        //        button7.Enabled = true;
        //        listView2.Enabled = false;
        //        foreach (var ctrl in _EditControls)
        //        {
        //            ctrl.Enabled = true;
        //        }
        //        listView1.Enabled = true;
        //        LoadPreset(_Presets[edit]);
        //    }
        //    else
        //    {
        //        button7.Enabled = false;
        //        listView2.Enabled = true;
        //        foreach (var ctrl in _EditControls)
        //        {
        //            ctrl.Enabled = false;
        //        }
        //        listView1.Enabled = false;
        //        LoadPresets();
        //    }
        //    listView2_ItemSelectionChanged(listView2, null);
        //}

        private void button12_Click(object sender, EventArgs e)
        {
            //var id = GetCurrentSelectedPresetId();
            //if (id == -1)
            //{
            //    return;
            //}
            //_Editing = id;
            //SwitchEditMode(true, id);
            var sel = GetCurrentSelectedPresetId();
            if (sel == -1) return;
            _Editing = sel;
            RefreshControls();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //if (_Editing != -1)
            //{
            //    _Presets[_Editing].GetPluginOptions(Options);
            //    _Presets[_Editing].Mods = String.Join(",",
            //        listView1.Items.OfType<ListViewItem>()
            //        .Where(i => i.Checked)
            //        .Select(i => ((PluginContainer)i.Tag).AssemblyName));
            //}
            //SwitchEditMode(false, -1);
            var sel = _Editing;
            _Editing = -1;
            if (sel == -1 || !_Presets[sel].Editable)
            {
                RefreshControls();
                return;
            }
            _Presets[sel].GetPluginOptions(Options, null);
            _Presets[sel].Mods = String.Join(",",
                listView1.Items.OfType<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => ((PluginContainer)i.Tag).AssemblyName));
            if (sel != 0) _Presets[sel].Name = textBox1.Text;
            RefreshControls();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (_Editing != -1) return;
            string n;
            for (int i = 1; ; ++i)
            {
                n = "Preset " + i.ToString();
                if (!_Presets.Any(pp => pp.Name == n))
                {
                    break;
                }
            }
            var p = new Preset(n, true);
            _Presets.Add(p);
            listView2.Items.Add(new ListViewItem(n) { Tag = p });
            RefreshControls();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var id = GetCurrentSelectedPresetId();
            if (id == -1 || id == 0 || _Editing != -1)
            {
                return;
            }
            _Presets.RemoveAt(id);
            listView2.Items.RemoveAt(id);
            RefreshControls();
        }

        private void listView2_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            //LoadPresets();
            if (_Editing == -1) RefreshControls();
        }

        private void listView2_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            //var id = GetCurrentSelectedPresetId();
            //if (id == 0 || id == -1)
            //{
            //    textBox1.Enabled = false;
            //    textBox1.Text = "";
            //    button9.Enabled = false;
            //    button12.Enabled = id == 0;
            //}
            //else
            //{
            //    textBox1.Enabled = _Presets[id].Editable;
            //    textBox1.Text = e.Item.Text;
            //    button9.Enabled = true;
            //    button12.Enabled = true;
            //}
            if (_Editing == -1) RefreshControls();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var id = _Editing;
            if (id == -1 || id != 0 || !_Presets[id].Editable)
            {
                return;
            }
            _Presets[id].Name = textBox1.Text;
            //Don't need to refresh
        }

        #endregion

        private void Button10_Click(object sender, EventArgs e)
        {
            var dialog = new CreateShortcutDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var args = GetShortcutOptions(dialog.Mode);
                var selectedPresets = string.Join(",", GetCheckedPresetIds().Select(id => _Presets[id].Name));
                var defaultPresetMods = _Presets[0].Mods;
                var defaultPresetOptions = _Presets[0].Options;
                var sb = new StringBuilder();
                sb.Append(selectedPresets);
                sb.Append(';');
                sb.Append(defaultPresetMods);
                foreach (var oo in defaultPresetOptions)
                {
                    if (oo.Item1 == null) continue;
                    sb.Append(';');
                    sb.Append(oo.Item1);
                    if (oo.Item2 != null)
                    {
                        sb.Append('=');
                        sb.Append(oo.Item2);
                    }
                }
                args.Save(dialog.FileName, sb.ToString(), false);
            }
        }
    }
}
