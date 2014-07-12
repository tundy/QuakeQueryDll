using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuakeQueryDll
{
    public class Query
    {
        public ushort PortNumber;
        public IPAddress IP;

        private IPEndPoint SERVER;
        private UdpClient socket;

        private static readonly byte[] prefix = new byte[] { 255, 255, 255, 255 };
        private static readonly string s_prefix = Encoding.UTF8.GetString(prefix);

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

            SERVER = new IPEndPoint(IP, PortNumber);
            SetLocalPort(out socket);
            socket.Client.ReceiveTimeout = 125;
            try
            {
                socket.Connect(SERVER);
            }
            catch (SocketException ex)
            {
                socket.Close();
                throw new SocketException(ex.ErrorCode);
            }

            //Change String to Byte[] and add 4*255 bytes on start
            RawCMD = Encoding.UTF8.GetBytes(cmd);
            Array.Resize(ref RawCMD, RawCMD.Length + 4);
            System.Buffer.BlockCopy(RawCMD, 0, RawCMD, 4, RawCMD.Length - 4);
            System.Buffer.BlockCopy(prefix, 0, RawCMD, 0, 4);

            socket.Send(RawCMD, RawCMD.Length);

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
                    RawData = socket.Receive(ref SERVER);
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
                socket.Close();
                throw;
            }
            
            bool EOT = false;
            byte[] RawData = null;
            while(!EOT)
            {
                try
                {
                    RawData = Recv(3);
                }
                catch
                {
                    socket.Close();
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
                    int i = RawData.Length - 1;
                    while (RawData[i] == 0)
                        i--;
                    RawData[i-3] = 0;
                }

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

            return ServerList;
        }
        public string Out(string cmd)
        {
            try
            {
                Send(cmd);
            }
            catch
            {
                socket.Close();
                throw;
            }

            string receivedData = String.Empty;
            byte[] RawData = null;
            try
            {
                RawData = Recv(3);
            }
            catch
            {
                socket.Close();
                throw;
            }

            if (RawData != null)
            {
                receivedData += Encoding.UTF8.GetString(RawData);
                receivedData = FixNewLines(receivedData);
            }
            
            if (RawData != null)
                receivedData = "Receive data from " + SERVER.ToString() + Environment.NewLine + receivedData + Environment.NewLine;
            else
                receivedData = "No data from " + SERVER.ToString() + Environment.NewLine + receivedData + Environment.NewLine;

            socket.Close();

            receivedData = RemovePrefix(receivedData);
            return receivedData;
        }
        public string Rcon(string rcon, string cmd)
        {
            if (rcon.Length == 0)
                throw new Win32Exception(QueryError.NoImput, "Rcon password is not set.");
            return Out("rcon " + rcon + " " + cmd);
        }
        public string Print(string rcon, string text)
        {
            return Rcon(rcon, " \"" + text + "\"");
        }
        public string Say(string rcon, string text)
        {
            return Rcon(rcon, " say \"" + text + "\"");
        }
        public string BigText(string rcon, string text)
        {
            return Rcon(rcon, " bigtext \"" + text + "\"");
        }
        public string PM(string rcon, string id, string text)
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
            return PM(rcon, playerID, text);
        }
        public string PM(string rcon, int id, string text)
        {
            return Rcon(rcon, "tell " + id.ToString() + " \"" + text + "\"");
        }
    }
}