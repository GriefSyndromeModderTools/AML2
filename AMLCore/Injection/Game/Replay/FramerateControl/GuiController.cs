using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AMLCore.Injection.Game.Replay.FramerateControl
{
    public partial class GuiController : Form
    {
        public GuiController()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChangeByButton(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ChangeByButton(2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ChangeByButton(5);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChangeByButton(10);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ChangeByButton(20);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ChangeByButton(50);
        }

        private void GuiController_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        private void ChangeByButton(float val)
        {
            FramerateHelper.Ratio = val;
            UpdateScroolBar(val);
            textBox1.Text = val.ToString("0.00");
        }

        private void UpdateScroolBar(float val)
        {
            if (val < 5)
            {
                val = (val - 1) / 4 * 100;
            }
            else
            {
                val = (val - 5) / 45 * 100 + 100;
            }
            if (val > 200) val = 200;
            if (val < 0) val = 0;
            trackBar1.Value = (int)Math.Round(val);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float val = trackBar1.Value;
            if (val < 100)
            {
                val = 1 + val / 100 * 4;
            }
            else
            {
                val = 5 + (val - 100) / 100 * 45;
            }
            FramerateHelper.Ratio = val;
            textBox1.Text = val.ToString("0.00");
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Single.TryParse(textBox1.Text, out var val) && val >= 1 && val <= 50)
                {
                    FramerateHelper.Ratio = val;
                    textBox1.Text = val.ToString("0.00");
                    UpdateScroolBar(val);
                }
                else
                {
                    textBox1.Text = FramerateHelper.Ratio.ToString("0.00");
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                textBox1.Text = FramerateHelper.Ratio.ToString("0.00");
                e.Handled = true;
            }
        }
    }
}
