using System;
using System.Windows.Forms;

namespace iLiberate_indev
{
    public partial class LogWindow : Form
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        public void AddLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddLog), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            textBoxLog.AppendText($"[{timestamp}] {message}\r\n");
            textBoxLog.SelectionStart = textBoxLog.Text.Length;
            textBoxLog.ScrollToCaret();
        }

        public void ClearLog()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ClearLog));
                return;
            }

            textBoxLog.Clear();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearLog();
        }
    }
}

