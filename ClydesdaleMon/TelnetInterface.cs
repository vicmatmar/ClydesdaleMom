// minimalistic telnet implementation
// conceived by Tom Janssens on 2007/06/06  for codeproject
//
// http://www.corebvba.be



using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace MinimalisticTelnet
{
    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    enum Options
    {
        SGA = 3
    }

    public class TelnetConnection : IDisposable
    {
        TcpClient _tcpSocket;
        int _timeOutMs = 100;

        /// <summary>
        /// Initializes a new instance of the System.Net.Sockets.TcpClient class and 
        /// connects to the specified port on the specified host.
        /// </summary>
        /// <param name="Hostname"></param>
        /// <param name="Port"></param>
        public TelnetConnection(string Hostname, int Port)
        {
            _tcpSocket = new TcpClient(Hostname, Port);
        }

        public void Dispose()
        {
            if (_tcpSocket != null)
                _tcpSocket.Close();
        }

        public void Close()
        {
            _tcpSocket.Close();
        }

        public string Login(string Username, string Password, int LoginTimeOutMs)
        {
            int oldTimeOutMs = _timeOutMs;
            _timeOutMs = LoginTimeOutMs;
            string s = Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no login prompt");
            WriteLine(Username);

            s += Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no password prompt");
            WriteLine(Password);

            s += Read();
            _timeOutMs = oldTimeOutMs;
            return s;
        }

        public void WriteLine(string cmd)
        {
            Write(cmd + "\n");
        }

        public void Write(string cmd)
        {
            if (!_tcpSocket.Connected) return;
            byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
            _tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }

        public string Read()
        {
            if (!_tcpSocket.Connected) return null;
            StringBuilder sb = new StringBuilder();
            do
            {
                ParseTelnet(sb);
                System.Threading.Thread.Sleep(_timeOutMs);
            } while (_tcpSocket.Available > 0);
            return sb.ToString();
        }

        public bool IsConnected
        {
            get { return _tcpSocket.Connected; }
        }

        void ParseTelnet(StringBuilder sb)
        {
            while (_tcpSocket.Available > 0)
            {
                int input = _tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case '^':
                        // Zebrashark keeps sending ^\r\n
                        input = _tcpSocket.GetStream().ReadByte();
                        input = _tcpSocket.GetStream().ReadByte();
                        break;
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = _tcpSocket.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = _tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                _tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA)
                                    _tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                else
                                    _tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                _tcpSocket.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }
    }
}
