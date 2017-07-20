using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Internal
{
    internal partial class UpdateWaitingWindow : Form
    {
        private readonly DownloadTask[] _Tasks;

        public UpdateWaitingWindow(DownloadTask[] tasks)
        {
            InitializeComponent();
            _Tasks = tasks;
            TotalKB = tasks.Sum(t => t.Size) / 1024;
        }

        private int _TotalKB;
        public int TotalKB
        {
            get => _TotalKB;
            set
            {
                _TotalKB = value;
                label2.Text = String.Format("{0}KB/{1}KB", _CurrentKB, _TotalKB);
            }
        }

        private int _CurrentKB;
        public int CurrentKB
        {
            get => _CurrentKB;
            set
            {
                _CurrentKB = value;
                label2.Text = String.Format("{0}KB/{1}KB", _CurrentKB, _TotalKB);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CoreLoggers.Update.Error("download cancelled");
            backgroundWorker1.CancelAsync();
            button1.Enabled = false;
            this.DialogResult = DialogResult.Cancel;
        }

        private void UpdateWaitingWindow_Shown(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private WebClient _Client = new WebClient();

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                foreach (var t in _Tasks)
                {
                    _Client.DownloadFile(t.Url, t.Destination);
                    backgroundWorker1.ReportProgress(0, t.Size);
                    if (backgroundWorker1.CancellationPending)
                    {
                        break;
                    }
                }
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ee)
            {
                CoreLoggers.Update.Error("cannot download file: {0}", ee.ToString());
                this.DialogResult = DialogResult.Cancel;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CoreLoggers.Update.Info("download work ends");
            this.Close();
        }
    }
}
