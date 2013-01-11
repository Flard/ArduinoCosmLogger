using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Net;

namespace ArduinoComLogger
{
    public partial class MainForm : Form
    {
        Thread readThread;
        static bool _continue;
        static System.IO.Ports.SerialPort serialPort;
        static string _apiKey;
        static string _listId;

        public MainForm()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            ArduinoComLogger.Properties.Settings.Default.Save();

            Application.Exit();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.cbComPort.Items.AddRange(SerialPort.GetPortNames());
            serialPort = new SerialPort();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.Hide();
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.Show();
        }

        protected void setState(bool enabled)
        {
            this.cbComPort.Enabled =
                this.tbCosmApiKey.Enabled =
                this.tbCosmStreamId.Enabled =
                    !enabled;

            this.startButton.Enabled = !enabled;
            this.stopButton.Enabled = enabled;
        }


        private void startButton_Click(object sender, EventArgs e)
        {
            setState(true);
            readThread = new Thread(ReadSerialPort);

            serialPort.PortName = cbComPort.Text;
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            _apiKey = tbCosmApiKey.Text;
            _listId = tbCosmStreamId.Text;

            //var values = new string[] { "200", "300", "400" };
            //PostValue(values);

            serialPort.Open();
            _continue = true;
            readThread.Start(this);

            AddToLog("Started listening on " + cbComPort.Text);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            AddToLog("Closing connection... ");
            _continue = false;
            readThread.Join();
            serialPort.Close();
            AddToLog("Connection closed");

            setState(false);
        }

        public static void ReadSerialPort(object data)
        {
            MainForm form = (MainForm)data;

            while (_continue)
            {
                try
                {
                    string message = serialPort.ReadLine();
                    form.AddToLog(message);
                    if (message[0] == '~') {
                        message = message.TrimStart('~');
                        PostValue(message);
                    }
                }
                catch (TimeoutException) { }
            }
        }

        public static void PostValue(string[] values)
        {
            PostValue(String.Join(",", values));
        }


        public static void PostValue(string value)
        {
             byte[] postArray = Encoding.ASCII.GetBytes(value);
            WebClient wc = new WebClient();
            wc.Headers.Add("X-PachubeApiKey", _apiKey);
            wc.UploadData("http://www.pachube.com/api/" + _listId + ".csv", "PUT", postArray);
        }

        delegate void AddToLogWindowDelegate(String msg);

        public void AddToLog(String message)
        {
            // Check whether the caller must call an invoke method when making method calls to listBoxCCNetOutput because the caller is 
            // on a different thread than the one the listBoxCCNetOutput control was created on.
            if (lbLog.InvokeRequired)
            {
                AddToLogWindowDelegate update = new AddToLogWindowDelegate(AddToLog);
                lbLog.Invoke(update, message);
            }
            else
            {
                lbLog.Items.Add(message);
                if (lbLog.Items.Count > 50)
                {
                    lbLog.Items.RemoveAt(0); // remove first line
                }
                // Make sure the last item is made visible
                lbLog.SelectedIndex = lbLog.Items.Count - 1;
                lbLog.ClearSelected();
            }
        }
    }
}
