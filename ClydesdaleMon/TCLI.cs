using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

using MinimalisticTelnet;

namespace PowerCalibration
{
    /// <summary>
    /// Class to handle telnet command line interfacing
    /// </summary>
    class TCLI
    {
        /// <summary>
        /// Simple structure used to return voltage/current value pair
        /// </summary>
        public struct Current_Voltage
        {
            public double Current;
            public double Voltage;

            public Current_Voltage(double i = 0.0, double v = 0.0)
            {
                Current = i;
                Voltage = v;
            }
        }

        /// <summary>
        /// Structure use to return pinfo tokens values
        /// </summary>
        public struct Tokens
        {
            public string EUI;
            public int VoltageGainToken;
            public int CurrentGainToken;

            public int VoltageFactor;
            public int CurrentFactor;

            public Tokens(int vgain = 0x004000000, int igain = 0x00400000, int ifactor = 15, int vfactor = 240, string eui = "")
            {
                EUI = eui;
                VoltageGainToken = vgain;
                CurrentGainToken = igain;
                VoltageFactor = vfactor;
                CurrentFactor = ifactor;
            }
        }

        public static Tokens Parse_Pinfo_Tokens(
            TelnetConnection telnet_connection, string cmd_prefix)
        {
            string vfactorPattern = "Voltage Factor: ([\\d]+)";
            string ifactorPattern = "Current Factor: ([\\d]+)";

            string vgainTokenPattern = "VGain Token\\s0x([0-9,A-F]{8})";
            string igainTokenPattern = "IGain Token\\s0x([0-9,A-F]{8})";

            string cmd = string.Format("cu {0}_pinfo", cmd_prefix);
            telnet_connection.WriteLine(cmd);
            Thread.Sleep(500);
            string datain = telnet_connection.Read();
            Trace.WriteLine(datain);
            string msg;

            Tokens tokens = new Tokens();
            if (datain != null && datain.Length > 0)
            {
                Match match = Regex.Match(datain, vfactorPattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for Voltage Factor.  Output was:{0}", datain);
                    throw new Exception(msg);
                }
                tokens.VoltageFactor = Convert.ToInt32(match.Groups[1].Value, 10);

                match = Regex.Match(datain, ifactorPattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for Current Factor.  Output was:{0}", datain);
                    throw new Exception(msg);
                }
                tokens.CurrentFactor = Convert.ToInt32(match.Groups[1].Value, 10);

                match = Regex.Match(datain, vgainTokenPattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for VGain Token.  Output was:{0}", datain);
                    throw new Exception(msg);
                }
                tokens.VoltageGainToken = Convert.ToInt32(match.Groups[1].Value, 16);

                match = Regex.Match(datain, igainTokenPattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for IGain Token.  Output was:{0}", datain);
                    throw new Exception(msg);
                }
                tokens.CurrentGainToken = Convert.ToInt32(match.Groups[1].Value, 16);

            }
            else
            {
                msg = string.Format("No data received after \"{0}\" command", cmd);
                throw new Exception(msg);
            }

            return tokens;
        }


        /// <summary>
        /// Sends a pload command and returns the current and voltage values
        /// </summary>
        /// <param name="telnet_connection">Already opened Telnet connection to the Ember</param>
        /// <param name="board_type">What board are we using</param>
        /// <returns>Current/Voltage structure values</returns>
        public static Current_Voltage Parse_Pload_Registers(
            TelnetConnection telnet_connection, string cmd_prefix, double voltage_ac_reference, double current_ac_reference)
        {
            string rawCurrentPattern = "Raw IRMS: ([0-9,A-F]{8})";
            string rawVoltagePattern = "Raw VRMS: ([0-9,A-F]{8})";
            double current_cs = 0.0;
            double voltage_cs = 0.0;

            TCLI.Wait_For_Prompt(telnet_connection);

            string cmd = string.Format("cu {0}_pload", cmd_prefix);
            telnet_connection.WriteLine(cmd);
            Thread.Sleep(500);

            string datain = telnet_connection.Read();
            Trace.WriteLine(datain);

            TCLI.Wait_For_Prompt(telnet_connection);

            string msg;

            if (datain != null && datain.Length > 0)
            {
                Match on_off_match = Regex.Match(datain, "Changing OnOff .*");
                if (on_off_match.Success)
                {
                    msg = on_off_match.Value;
                }

                Match match = Regex.Match(datain, rawCurrentPattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for current.  Output was:{0}", datain);
                    throw new Exception(msg);
                }

                string current_hexstr = match.Groups[1].Value;
                int current_int = Convert.ToInt32(current_hexstr, 16);
                current_cs = RegHex_ToDouble(current_int);
                current_cs = current_cs * current_ac_reference / 0.6;

                voltage_cs = 0.0;
                match = Regex.Match(datain, rawVoltagePattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for voltage.  Output was:{0}", datain);
                    throw new Exception(msg);
                }

                string voltage_hexstr = match.Groups[1].Value;
                int volatge_int = Convert.ToInt32(voltage_hexstr, 16);
                voltage_cs = RegHex_ToDouble(volatge_int);
                voltage_cs = voltage_cs * voltage_ac_reference / 0.6;

            }
            else
            {
                msg = string.Format("No data received after \"{0}\" command", cmd);
                throw new Exception(msg);
            }

            Current_Voltage current_voltage = new Current_Voltage(i: current_cs, v: voltage_cs);
            return current_voltage;
        }

        /// <summary>
        /// Converts a 24bit hex (3 bytes) CS register value to a double
        /// </summary>
        /// <example>
        /// byte[] rx_data = new byte[3];
        /// rx_data[2] = 0x5c;
        /// rx_data[1] = 0x28;
        /// rx_data[0] = 0xf6;
        /// Should return midrange =~ 0.36
        /// </example>
        /// <param name="rx_data">data byte array byte[2] <=> MSB ... byte[0] <=> LSB</param>
        /// <returns>range 0 <= value < 1.0</returns>
        public static double RegHex_ToDouble(int data)
        {
            // Maximum 1 =~ 0xFFFFFF
            // Max rms 0.6 =~ 0x999999
            // Half rms 0.36 =~ 0x5C28F6
            double value = ((double)data) / 0x1000000; // 2^24
            return value;
        }

        /// <summary>
        /// Converts a hex string (3 bytes) CS register vaue to a double
        /// </summary>
        /// <param name="hexstr"></param>
        /// <returns>range 0 <= value < 1.0</returns>
        /// <seealso cref="double RegHex_ToDouble(int data)"/>
        public static double RegHex_ToDouble(string hexstr)
        {
            int val_int = Convert.ToInt32(hexstr, 16);
            return RegHex_ToDouble(val_int); ;
        }

        /// <summary>
        /// Telnets to the Ember and prints custom commands
        /// Parses command list and tries to find the pload or pinfo comand prefix
        /// It is usually "cs5480_" in the case of SPDI or "cs5490_" in the case of UART comunications
        /// Exception is thrown if not pload command is found after typing "cu"
        /// </summary>
        /// <returns></returns>
        public static string Get_Custom_Command_Prefix(TelnetConnection telnet_connection)
        {
            string cmd_pre = null;

            int try_count = 0;
            string data = "";

            TCLI.Wait_For_Prompt(telnet_connection);
            while (true)
            {
                telnet_connection.WriteLine("cu");
                data += telnet_connection.Read();
                if (data.Contains("pload"))
                    break;
                if (try_count++ > 3)
                    break;
            }

            string msg = "";
            if (!data.Contains("pload"))
            {
                msg = string.Format("Unable to find pload command from custom command output list from Ember.  Output was: {0}", data);
                throw new Exception(msg);
            }

            string pattern = @"(cs[0-9]{4})_pload\r\n";
            Match match = Regex.Match(data, pattern);
            if (match.Groups.Count != 2)
            {
                msg = string.Format("Unable to parse custom command list for pload.  Output was:{0}", data);
                throw new Exception(msg);
            }

            cmd_pre = match.Groups[1].Value;
            return cmd_pre;
        }

        /// <summary>
        /// Gets the EUI
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <returns></returns>
        public static string Get_EUI(TelnetConnection telnet_connection)
        {
            string eui = null;
            int try_count = 0;
            string datain = "";
            string pattern = Regex.Escape("node [(>)") + "([0-9,A-F]{16})" + Regex.Escape("]");

            TCLI.Wait_For_Prompt(telnet_connection);
            while (true)
            {
                telnet_connection.WriteLine("info");
                Thread.Sleep(500);
                datain = telnet_connection.Read();
                if (datain != null && datain.Length > 0 && Regex.Match(datain, pattern).Groups.Count == 2)
                    break;
                if (try_count++ > 3)
                    break;
            }


            if (datain != null && datain.Length > 0)
            {
                Match match = Regex.Match(datain, pattern);
                if (match.Groups.Count != 2)
                {
                    string msg = string.Format("Unable to parse info EUI.  Output was:{0}", datain);
                    throw new Exception(msg);
                }
                eui = match.Groups[1].Value;
            }
            else
            {
                string msg = string.Format("No data received after \"Info\" command");
                throw new Exception(msg);
            }

            return eui;
        }

        /// <summary>
        /// Gets the MFG string
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <returns></returns>
        public static string Get_MFGString(TelnetConnection telnet_connection)
        {
            string mfgstr = null;
            string datain = "";
            string pattern = "MFG String: (\\S+)";

            TCLI.Wait_For_Prompt(telnet_connection);
            telnet_connection.WriteLine("info");
            Thread.Sleep(500);
            datain = telnet_connection.Read();


            if (datain != null && datain.Length > 0)
            {
                Match match = Regex.Match(datain, pattern);
                if (match.Groups.Count != 2)
                {
                    string msg = string.Format("Unable to parse info MFG String.  Output was:{0}", datain);
                    throw new Exception(msg);
                }
                mfgstr = match.Groups[1].Value;
            }
            else
            {
                string msg = string.Format("No data received after \"Info\" command");
                throw new Exception(msg);
            }

            return mfgstr;
        }

        /// <summary>
        /// Sets the state of the relay
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <param name="value"></param>
        public static void Set_Relay_State(TelnetConnection telnet_connection, bool value)
        {
            if (value)
            {
                Wait_For_Prompt(telnet_connection);
                telnet_connection.WriteLine("write 1 6 0 1 0x10 {01}");
                Wait_For_Prompt(telnet_connection);

            }
            else
            {
                telnet_connection.WriteLine("write 1 6 0 1 0x10 {00}");
            }
        }

        /// <summary>
        /// Just a wrapper that reads and returns the telnet data
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <returns></returns>
        public static string Read(TelnetConnection telnet_connection)
        {
            return telnet_connection.Read();
        }

        /// <summary>
        /// Wrapper for writeline
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <param name="text"></param>
        public static void WriteLine(TelnetConnection telnet_connection, string text)
        {
            telnet_connection.WriteLine(text);
        }

        /// <summary>
        /// Inputs a blank line and waits for the prompt
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <param name="prompt">The expected prompt.  Default '>'</param>
        /// <param name="sendEnter">Whether to send CR before waiting</param>
        /// <param name="retry_count">The max number of times that we read the session looking for the prompt</param>
        public static void Wait_For_Prompt(TelnetConnection telnet_connection, string prompt = ">", bool sendEnter = true, int retry_count = 5)
        {
            telnet_connection.Read();
            int n = 0;
            string data = "";
            while (n < retry_count)
            {
                if (sendEnter)
                {
                    telnet_connection.WriteLine("");
                    Thread.Sleep(200);
                }

                data = telnet_connection.Read();
                if (data.Contains(prompt))
                    break;
                n++;
            }

            if (n >= retry_count)
            {
                throw new Exception("Telnet session prompt not detected");
            }
        }

        /// <summary>
        /// Waits for output
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <param name="expected_data"></param>
        /// <param name="timeout_ms"></param>
        /// <param name="sample_ms"></param>
        /// <returns></returns>
        public static string Wait_For_String(TelnetConnection telnet_connection, string expected_data, int timeout_ms, int sample_ms = 250)
        {
            int total_wait = 0;
            string data = "";
            while (total_wait < timeout_ms)
            {
                data += telnet_connection.Read();
                if (data.Contains(expected_data))
                    break;
                Thread.Sleep(sample_ms);
                total_wait += sample_ms;
            }

            if (!data.Contains(expected_data))
            {
                string msg = string.Format("Telnet session timeout after {0} ms waiting for data \"{1}\". Data was: \"{2}\"",
                    total_wait, expected_data, data);
                throw new Exception(msg);
            }

            return data;
        }

        /// <summary>
        /// Sends a command and waits for specific message to be returned
        /// </summary>
        /// <param name="telnet_connection"></param>
        /// <param name="command"></param>
        /// <param name="expected_data"></param>
        /// <param name="retry_count"></param>
        /// <param name="delay_ms"></param>
        public static string Wait_For_String(TelnetConnection telnet_connection, string command, string expected_data, int retry_count = 3, int delay_ms = 100)
        {
            Wait_For_Prompt(telnet_connection);
            int n = 0;
            string data = "";
            while (n < retry_count)
            {
                telnet_connection.WriteLine(command);
                Thread.Sleep(delay_ms);
                data = telnet_connection.Read();
                if (data != null)
                {
                    if (data.Contains(expected_data))
                    {
                        break;
                    }
                }
                n++;
            }

            if (!data.Contains(expected_data))
            {
                string msg = string.Format("Telnet session data not detected after command \"{0}\".  Expected: \"{1}\". Received: \"{2}\"",
                    command, expected_data, data);
                throw new Exception(msg);
            }

            return data;
        }

        public static Match Wait_For_Match(TelnetConnection telnet_connection, string command, string pattern, int retry_count = 3, int delay_ms = 100)
        {
            Wait_For_Prompt(telnet_connection);
            int n = 0;
            string data = "";

            Match match;

            while (n < retry_count)
            {
                telnet_connection.WriteLine(command);

                Thread.Sleep(delay_ms);
                data = telnet_connection.Read();
                if (data != null)
                {
                    match = Regex.Match(data, pattern);

                    if (match.Success)
                    {
                        return match;
                    }
                }

                n++;
            }

            string msg = string.Format("Telnet session data not match found after command \"{0}\".  Expected: \"{1}\". Received: \"{2}\"",
                command, pattern, data);
            throw new Exception(msg);
        }


    }
}
