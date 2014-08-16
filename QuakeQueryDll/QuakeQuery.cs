using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QuakeQueryDll
{
    public partial class QuakeQuery
    {
        public int Port { get { return ((IPEndPoint)Socket.Client.LocalEndPoint).Port; } }
        public ConcurrentDictionary<string, Server> Servers { get { return _servers; } internal set { _servers = value; } }
        private ConcurrentDictionary<string, Server> _servers = new ConcurrentDictionary<string, Server>();
        private Thread _recThread;
        internal UdpClient Socket;


        public QuakeQuery()
        {
            FindNearestPort(27900);
        }
        public void ClearList()
        {
            _servers.Clear();
        }
        public void Close()
        {
            if (_recThread != null) _recThread.Abort();
            if (Socket != null) Socket.Close();
        }

        public void ChangePort(int newPort)
        {
            Close();
            Init(newPort);
        }
        public void FindNearestPort(int port)
        {
            if (port > 28000 || port < 26000) port = 26000;
            Close();
            try
            {
                Init(port);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    FindNearestPort(++port);
                else
                    throw;
            }
        }
        internal void Init(int newPort)
        {
            try
            {
                Socket = new UdpClient(newPort);
            }
            catch (SocketException)
            {
                if (Socket != null)
                    Socket.Close();
                throw;
            }
            _recThread = new Thread(new Receiver(this).Loop)
            {
                IsBackground = true,
                Name = "Receiver :" + Port
            };
            _recThread.Start();
        }

        /// <summary>
        /// Make sure that new line character is same
        /// </summary>
        internal static string FixNewLines(string text)
        {
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");
            //text = text.Replace("\n", Environment.NewLine);
            return text;
        }

        internal void DecodeServerList(byte[] bytes)
        {
            // Check last bytes if there is EOT
            // Remove Slash before EOT so he would know that there are no more servers
            {
                var i = bytes.Length - 1;
                while (bytes[i] == 0)
                    i--;
                if (i >= 3 && bytes[i] == 84 && bytes[i - 1] == 79 && bytes[i - 2] == 69 && bytes[i - 3] == 92)
                    bytes[i - 3] = 0;
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 92) continue;
                var ip = new StringBuilder();
                ip.Append(bytes[++i].ToString(CultureInfo.InvariantCulture));
                ip.Append(".");
                ip.Append(bytes[++i].ToString(CultureInfo.InvariantCulture));
                ip.Append(".");
                ip.Append(bytes[++i].ToString(CultureInfo.InvariantCulture));
                ip.Append(".");
                ip.Append(bytes[++i].ToString(CultureInfo.InvariantCulture));
                var port = bytes[++i] << 8;
                port += bytes[++i];

                // Add Server if does not exist
                var senderId = ip + ":" + port;
                Server server;
                if (_servers.TryGetValue(senderId, out server)) continue;
                server = new Server(ip.ToString(), port);
                _servers.TryAdd(senderId, server);
                OnNewServerResponse(server);
            }
        }        
    }
}
