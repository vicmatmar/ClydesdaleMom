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

        uint _read_count = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            labelSensorMonitor.Text = "";
            labelSensorMaxMinDetected.Text = "";
            labelSensorId.Text = "";
        }

        /// <summary>
        /// Creates and starts task
        /// </summary>
        void start_task()
        {
            _telnet_connection = new TelnetConnection(Properties.Settings.Default.Ember_Interface_IP_Address, 4900);
            labelSensorMaxMinDetected.Text = "Telnet started @ " + Properties.Settings.Default.Ember_Interface_IP_Address;

            _cancel = new CancellationTokenSource();
            _monitor_task = new Task(monitor_run, _cancel.Token);
            _monitor_task.ContinueWith(monitor_completed, TaskContinuationOptions.OnlyOnRanToCompletion);
            _monitor_task.ContinueWith(monitor_error, TaskContinuationOptions.OnlyOnFaulted);

            _monitor_task.Start();
        }

        /// <summary>
        /// Main task function.  
        /// Monitors the sensors level
        /// </summary>
        void monitor_run()
        {
            _read_count = 0;

            // Clear sensor id
            controlSetPropertyValue(labelSensorId, "Clear Sensor id");
            Match m = TCLI.Wait_For_Match(_telnet_connection, "cu si cl", "New Value: 0", 2);
            // Wait to pair
            controlSetPropertyValue(labelSensorId, "Waiting to pair");
            while (true)
            {
                if (_cancel.Token.IsCancellationRequested)
                    return;

                // Read id
                m = TCLI.Wait_For_Match(_telnet_connection, "cu si re", "Sensor ID: ([0-9, A-F]+)", 2, 100);
                if (m.Success)
                {
                    int id = 0;
                    try
                    {
                        id = Convert.ToInt32(m.Groups[1].Value, 16);
                    }
                    catch { }
                    if (id != 0)
                    {
                        controlSetPropertyValue(labelSensorId, m.Groups[0].Value);
                        break;
                    }
                }
            }

            // Calibrate
            controlSetPropertyValue(labelSensorMonitor, "Press Calibration button");
            string data = "";
            while (true)
            {
                if (_cancel.Token.IsCancellationRequested)
                    return;

                data += TCLI.Read(_telnet_connection);
                if (data.Contains("CMD_CALIBRATION_START"))
                {
                    Thread.Sleep(500);
                    break;
                }
            }

            controlSetPropertyValue(labelSensorMonitor, "Turn device 180 degrees AND WAIT");
            data = "";
            while (true)
            {
                if (_cancel.Token.IsCancellationRequested)
                    return;

                data += TCLI.Read(_telnet_connection);
                if (data.Contains("CMD_CALIBRATION_COMPLETE"))
                {
                    controlSetPropertyValue(labelSensorMonitor, "CMD_CALIBRATION_COMPLETE");
                    break;
                }
            }

            while (true)
            {
                if (_cancel.Token.IsCancellationRequested)
                    return;

                m = TCLI.Wait_For_Match(_telnet_connection, "cu lev get", "Current = ([0-9]+)", 2, 100);
                if (m.Success)
                {
                    _read_count++;
                    string lbltxt = string.Format("{0} - {1}", 
                        DateTime.Now.ToString("hh:mm:ss"), m.Groups[0].Value);
                    controlSetPropertyValue(labelSensorMonitor, lbltxt);
                    try
                    {
                        int val = Convert.ToInt32(m.Groups[1].Value);
                        if (val == 0)
                            controlSetPropertyValue(labelSensorMaxMinDetected, "Min detected");
                        if (val == 254)
                            controlSetPropertyValue(labelSensorMaxMinDetected, "Max detected");

                    }
                    catch (Exception ex)
                    {
                        controlSetPropertyValue(labelSensorMaxMinDetected, ex.Message);
                    }
                }

            }

        }

        /// <summary>
        /// Called when tasks completes
        /// </summary>
        /// <param name="task"></param>
        void monitor_completed(Task task)
        {
            _telnet_connection.Close();
            controlSetPropertyValue(button1, "Start");
        }

        /// <summary>
        /// Called when running task throws exception
        /// </summary>
        /// <param name="task"></param>
        void monitor_error(Task task)
        {
            _telnet_connection.Close();

            controlSetPropertyValue(button1, "Start");
            controlSetPropertyValue(labelSensorMaxMinDetected, task.Exception.InnerException.Message);
        }

        /// <summary>
        /// Cancels running task
        /// </summary>
        void cancel_task()
        {
            if (_cancel != null && _cancel.Token.CanBeCanceled)
                _cancel.Cancel();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                button1.Text = "Stop";
                labelSensorId.Text = "";
                labelSensorMonitor.Text = "";
                labelSensorMaxMinDetected.Text = "";

                start_task();
            }
            else
            {
                cancel_task();
            }

        }

        /// <summary>
        /// Use to set GUI element properties values when running from a different thread
        /// </summary>
        /// <param name="control"></param>
        /// <param name="value"></param>
        /// <param name="property_name"></param>
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
