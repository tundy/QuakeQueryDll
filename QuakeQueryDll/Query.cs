using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace QuakeQueryDll
{
    public class Query
    {
        public ushort PortNumber;
        public IPAddress IP;

        private IPEndPoint _server;
        private UdpClient _socket;

        private static readonly byte[] b_prefix = new byte[] { 255, 255, 255, 255 };
        private static readonly string s_prefix = Encoding.UTF8.GetString(b_prefix);

        public Query()
        {
        }
        public Query(IPAddress ip, int port)
        {
            IP = ip;
            PortNumber = (ushort)port;
        }
        public Query(IPAddress ip, ushort port)
        {
            IP = ip;
            PortNumber = port;
        }
        public Query(string ip, int port)
        {
            if (!IPAddress.TryParse(ip, out IP))
                IP = IPAddress.Parse("127.0.0.1");
            PortNumber = (ushort)port;
        }
        public Query(string ip, ushort port)
        {
            if (!IPAddress.TryParse(ip, out IP))
                IP = IPAddress.Parse("127.0.0.1");
            PortNumber = port;
        }
        public Query(string ip, string port)
        {
            if (!IPAddress.TryParse(ip, out IP))
                IP = IPAddress.Parse("127.0.0.1");
            if (!ushort.TryParse(port, out PortNumber))
                PortNumber = 27960;
        }
        public Query(Server server)
        {
            IP = server.IP;
            PortNumber = server.Port;
        }

        private void SetLocalPort(out UdpClient socket)
        {
            SetLocalPort(out socket, 27960);
        }
        private void SetLocalPort(out UdpClient socket, ushort port)
        {
            // Not in Quake 3 Network Protocol Range ?
            // just use anything else
            if (port > 28000 || port < 26000)
                socket = new UdpClient();
            try
            {
                socket = new UdpClient(port);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    SetLocalPort(out socket, (ushort)(port + 1));
                else
                    throw new Exception(ex.ToString(), ex);
            }
        }

        private void Send(string cmd)
        {
            byte[] RawCMD;

            _server = new IPEndPoint(IP, PortNumber);
            SetLocalPort(out _socket);
            _socket.Client.ReceiveTimeout = 125;
            try
            {
                _socket.Connect(_server);
            }
            catch (SocketException ex)
            {
                _socket.Close();
                throw new SocketException(ex.ErrorCode);
            }

            //Change String to Byte[] and add 4*255 bytes on start
            RawCMD = Encoding.UTF8.GetBytes(cmd);
            Array.Resize(ref RawCMD, RawCMD.Length + 4);
            System.Buffer.BlockCopy(RawCMD, 0, RawCMD, 4, RawCMD.Length - 4);
            System.Buffer.BlockCopy(b_prefix, 0, RawCMD, 0, 4);

            _socket.Send(RawCMD, RawCMD.Length);

        }
        private byte[] Recv(int Attempts)
        {
            int curAttempt = 0;
            int maxAttempts = Attempts;
            byte[] RawData = null;

            bool done = false;
            while (!done)
            {
                if (curAttempt++ == maxAttempts)
                    break;

                try
                {
                    RawData = _socket.Receive(ref _server);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                        continue;

                    throw;
                }

                done = true;
            }
            
            if (!done)
                return null;
            return RawData;
        }

        private string FixNewLines(string text)
        {
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");
            text = text.Replace("\n", Environment.NewLine);
            return text;
        }
        private string RemovePrefix(string text)
        {
            string pattern;
            //pattern = s_prefix + ".*" + Environment.NewLine;
            pattern = s_prefix + "print" + Environment.NewLine;
            text = Regex.Replace(text, pattern, "");
            pattern = s_prefix + "print";
            text = Regex.Replace(text, pattern, "");
            pattern = s_prefix + Environment.NewLine;
            text = Regex.Replace(text, pattern, "");
            pattern = s_prefix;
            text = Regex.Replace(text, pattern, "");
            return text;
        }

        public List<Server> Master(int protocol, bool full, bool empty)
        {
            List<Server> ServerList = new List<Server>(); 
            string extra = String.Empty;
            if (full)
                extra += " full";
            if (empty)
                extra += " empty";
            try
            {
                Send("getservers " + protocol.ToString() + extra);
            }
            catch
            {
                _socket.Close();
                throw;
            }
            
            bool EOT = false;
            byte[] RawData = null;
            while (!EOT)
            {
                try
                {
                    RawData = Recv(3);
                }
                catch
                {
                    _socket.Close();
                    throw;
                }

                if (RawData != null)
                {
                    // Check last bytes if there is EOT
                    int i = RawData.Length - 1;
                    while (RawData[i] == 0)
                        i--;
                    if (RawData[i] == 84 && RawData[i - 1] == 79 && RawData[i - 2] == 69)
                        EOT = true;
                }

                if (EOT)
                {
                    // Remove Slash before EOT so he would know that there is no more servers
                    int i = RawData.Length - 1;
                    while (RawData[i] == 0)
                        i--;
                    RawData[i - 3] = 0;
                }

                if (RawData != null)
                {
                    bool server = false;
                    string ip;
                    int port;
                    for (int i = 0; i < RawData.Length; i++)
                    {
                        if (RawData[i] == 92)
                            server = true;

                        if (server)
                        {
                            ip = RawData[++i].ToString();
                            ip += "." + RawData[++i].ToString();
                            ip += "." + RawData[++i].ToString();
                            ip += "." + RawData[++i].ToString();
                            port = RawData[++i] * 256;
                            port += RawData[++i];
                            ServerList.Add(new Server(ip, port));
                            server = false;
                        }
                    }
                }
            }
            if (ServerList == null || ServerList.Count == 0)
                return null;
            return ServerList;
        }
        public string Out(string cmd)
        {
            return Out(cmd, 3);
        }
        public string Out(string cmd, int Attempts)
        {
            return Out(cmd, Attempts, true);
        }
        private string Out(string cmd, int Attempts, bool WaitForEOT)
        {
            try
            {
                Send(cmd);
            }
            catch
            {
                _socket.Close();
                throw;
            }
            bool EOT = true;
            if (WaitForEOT)
                EOT = false;

            string receivedData = String.Empty;
            byte[] RawData = null;
            do
            {
                try
                {
                    RawData = Recv(Attempts);
                }
                catch
                {
                    _socket.Close();
                    throw;
                }

                if (RawData != null)
                {
                    receivedData += Encoding.UTF8.GetString(RawData);
                    receivedData = FixNewLines(receivedData);
                }
                else
                {
                    EOT = true;
                }
            }
            while (!EOT);

            _socket.Close();

            if (receivedData == null)
                return null;
            receivedData = RemovePrefix(receivedData);
            return receivedData;
        }
        public Dictionary<string, string> GetInfo()
        {
            return GetInfo(2);
        }
        public Dictionary<string, string> GetInfo(int Attempts)
        {
            var info = new Dictionary<string, string>();

            var tmpOut = Out("getinfo", Attempts, false);
            var var = tmpOut.Split('\\');
            int vars = var.Length;
            for (int i = 1; i < vars; i++)
                info.Add(var[i], var[++i]);
            return info;
        }
        public Dictionary<string, string> GetStatus()
        {
            return GetStatus(2);
        }
        public Dictionary<string, string> GetStatus(int Attempts)
        {
            var info = new Dictionary<string, string>();

            var tmpOut = Out("getstatus", Attempts, false);
            var var = tmpOut.Split('\\');
            int vars = var.Length;
            for (int i = 1; i < vars; i++)
                info.Add(var[i], var[++i]);
            return info;
        }
        /// <summary>
        /// Send rcon command and try to 'regex' cvar's value.
        /// Returns string if successfull and null if not.
        /// </summary>
        /// <param name="rcon">Rcon Password</param>
        /// <param name="cvar">Console Variable</param>
        /// <returns>Value of cvar</returns>
        public string GetCvar(string rcon, string cvar)
        {
            return GetCvar(rcon, cvar, 3);
        }
        /// <summary>
        /// Send rcon command and try to 'regex' cvar's value.
        /// Returns string if successfull and null if not.
        /// </summary>
        /// <param name="rcon">Rcon Password</param>
        /// <param name="cvar">Console Variable</param>
        /// <param name="Attempts">Number of atempts to comunicate with server</param>
        /// <returns>Value of cvar</returns>
        public string GetCvar(string rcon, string cvar, int Attempts)
        {
            cvar = RemoveSpaces(cvar);
            var input = Rcon(rcon, cvar, Attempts);
            var pattern = "\".+\"\\s+is:\"(.*)\\^7\"\\s+default:.*";
            var tmp = Regex.Match(input, pattern);
            if (!tmp.Success)
            {
                pattern = "\".+\"\\s+is:\"(.*)\\^7\"";
                tmp = Regex.Match(input, pattern);
                if (tmp.Success)
                    return tmp.Groups[1].Value;
                return null;
            }
            return tmp.Groups[1].Value;
        }
        /// <summary>
        /// Send rcon command to server.
        /// Returns string if successfull and null if not.
        /// </summary>
        /// <param name="rcon">Rcon Password</param>
        /// <param name="cmd">Rcon Command</param>
        /// <returns>Server's output if any</returns>
        public string Rcon(string rcon, string cmd)
        {
            return Rcon(rcon, cmd, 2);
        }
        /// <summary>
        /// Send rcon command to server.
        /// Returns string if successfull and null if not.
        /// </summary>
        /// <param name="rcon">Rcon Password</param>
        /// <param name="cmd">Rcon Command</param>
        /// <param name="Attempts">Number of atempts to comunicate with server</param>
        /// <returns>Server's output if any</returns>
        public string Rcon(string rcon, string cmd, int Attempts)
        {
            if (rcon.Length == 0)
                throw new Win32Exception(QueryError.NoImput, "Rcon password is not set.");
            return Out("rcon " + rcon + " " + cmd.Trim());
        }
        public string Print(string rcon, string text)
        {
            return Print(rcon, text, 3);
        }
        public string Print(string rcon, string text, int Attempts)
        {
            return Rcon(rcon, "\"" + text + "\"", Attempts);
        }
        public string Say(string rcon, string text)
        {
            return Say(rcon, text, 3);
        }
        public string Say(string rcon, string text, int Attempts)
        {
            return Rcon(rcon, "say \"" + text + "\"", Attempts);
        }
        public string BigText(string rcon, string text)
        {
            return BigText(rcon, text, 3);
        }
        public string BigText(string rcon, string text, int Attempts)
        {
            return Rcon(rcon, "bigtext \"" + text + "\"", Attempts);
        }
        public string PM(string rcon, string id, string text)
        {
            return PM(rcon, id, text, 3);
        }
        public string PM(string rcon, string id, string text, int Attempts)
        {
            int playerID = 0;
            if (id.Length > 0)
            {
                if (!Int32.TryParse(id, out playerID))
                    throw new Win32Exception(QueryError.WrongFormat, "ID is not in the correct format.");
            }
            else
            {
                throw new Win32Exception(QueryError.NoImput, "ID is not set.");
            }
            return PM(rcon, playerID, text, Attempts);
        }
        public string PM(string rcon, int id, string text)
        {
            return PM(rcon, id, text, 3);
        }
        public string PM(string rcon, int id, string text, int Attempts)
        {
            return Rcon(rcon, "tell " + id.ToString() + " \"" + text + "\"", Attempts);
        }

        private string RemoveSpaces(string text)
        {
            text = text.Replace(" ", "");
            return text.Replace("\t", "");
        }
    }
}