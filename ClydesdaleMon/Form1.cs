using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using MinimalisticTelnet;
using PowerCalibration;
using System.Text.RegularExpressions;

namespace ClydesdaleMon
{
    public partial class Form1 : Form
    {
        TelnetConnection _telnet_connection; // Telnet connection to ISA3 Adapter

        Task _monitor_task;
        CancellationTokenSource _cancel;

        delegate void setControlPropertyValueCallback(Control control, object value, string property_name);  // Set object text

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = "";
            label2.Text = "";
        }

        void start_task()
        {
            button1.Text = "Stop";
            _telnet_connection = new TelnetConnection(Properties.Settings.Default.Ember_Interface_IP_Address, 4900);
            _cancel = new CancellationTokenSource();
            _monitor_task = new Task(monitor_run, _cancel.Token);
            _monitor_task.ContinueWith(monitor_completed, TaskContinuationOptions.OnlyOnRanToCompletion);
            _monitor_task.ContinueWith(monitor_error, TaskContinuationOptions.OnlyOnFaulted);

            _monitor_task.Start();
        }

        void monitor_run()
        {
            while (true)
            {
                if (_cancel.Token.IsCancellationRequested)
                    return;

                Match m = TCLI.Wait_For_Match(_telnet_connection, "cu lev get", "Current = ([0-9]+)", 2, 100);
                if (m.Success)
                {
                    try
                    {
                        string lbltxt = string.Format("{0} - {1}", DateTime.Now.ToString("hh:mm:ss"), m.Groups[0].Value);
                        controlSetPropertyValue(label1, lbltxt);

                        int val = Convert.ToInt32(m.Groups[1].Value);
                        if (val == 0)
                            controlSetPropertyValue(label2, "Min detected");
                        if (val == 254)
                            controlSetPropertyValue(label2, "Max detected");

                    }
                    catch (Exception ex)
                    {
                        string msg = ex.Message;
                    }
                }

            }

        }

        void monitor_completed(Task task)
        {
            _telnet_connection.Close();
            button1.Text = "Start";
        }

        void monitor_error(Task task)
        {
            _telnet_connection.Close();
            button1.Text = "Start";
            string text = task.Exception.InnerException.Message;
        }

        void cancel_task()
        {
            if (_cancel != null && _cancel.Token.CanBeCanceled)
                _cancel.Cancel();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                start_task();
            }
            else
            {
                cancel_task();
            }

        }

        void controlSetPropertyValue(Control control, object value, string property_name = "Text")
        {
            if (control.InvokeRequired)
            {
                setControlPropertyValueCallback d = new setControlPropertyValueCallback(controlSetPropertyValue);
                this.Invoke(d, new object[] { control, value, property_name });
            }
            else
            {
                var property = control.GetType().GetProperty(property_name);
                if (property != null)
                {
                    property.SetValue(control, value);
                }
            }
        }



    }
}
