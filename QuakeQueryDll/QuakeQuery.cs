using System.Collections.Concurrent;
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
        private Receiver _receiver;
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
            _receiver.Work = false;
            _recThread.Abort();
            Socket.Close();
        }
        public void ChangePort(int newPort)
        {
            Close();
            Init(newPort);
        }
        public void FindNearestPort(int port)
        {
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
            _receiver = new Receiver(this);
            _recThread = new Thread(_receiver.Loop) {IsBackground = true};
            _recThread.Start();
        }

        internal static string FixNewLines(string text)
        {
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");
            //text = text.Replace("\n", Environment.NewLine);
            return text;
        }

        internal void DecodeServerList(byte[] bytes)
        {
            {
                // Check last bytes if there is EOT
                var i = bytes.Length - 1;
                while (bytes[i] == 0)
                    i--;
                if (i >= 3 && bytes[i] == 84 && bytes[i - 1] == 79 && bytes[i - 2] == 69 && bytes[i - 3] == 92)
                {
                    // Remove Slash before EOT so he would know that there are no more servers
                    bytes[i - 3] = 0;
                }
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 92) continue;
                var ip = new StringBuilder();
                ip.Append(bytes[++i].ToString());
                ip.Append(".");
                ip.Append(bytes[++i].ToString());
                ip.Append(".");
                ip.Append(bytes[++i].ToString());
                ip.Append(".");
                ip.Append(bytes[++i].ToString());
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
