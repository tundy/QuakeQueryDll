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
        #region Fields & Properties

        #region Properties
        public int Port
        {
            get { return ((IPEndPoint) Socket.Client.LocalEndPoint).Port; }
            set { Close(); Init(value); }
        }

        public ConcurrentDictionary<string, Server> Servers { get; internal set; } = new ConcurrentDictionary<string, Server>();
        #endregion Properties

        internal UdpClient Socket;
        private Thread _recThread;

        #endregion Fields & Properties

        #region Constructors
        public QuakeQuery(int port)
        {
            Port = port;
        }

        public QuakeQuery()
        {
            FindNearestPort(27900);
        }
        #endregion Constructors

        #region Destructor
        ~QuakeQuery() { Close(); }
        #endregion Destructor

        #region Methods

        #region Public Methods
        public void ClearList()
        {
            Servers.Clear();
        }

        public void Close()
        {
            _recThread?.Abort();
            Socket?.Close();
        }

        public void FindNearestPort(int port)
        {
            //if (port > 28000 || port < 26000) port = 26000;
            Close();
            try
            {
                Init(port);
            }
            catch (SocketException ex)
            {
                if (port >= 65535)  // Stop somewhere
                {
                    throw;
                }
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    FindNearestPort(port + 1);
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion Public Methods

        #region Internal Methods

        private void Init(int newPort)
        {
            try
            {
                Socket = new UdpClient(newPort);
            }
            catch (SocketException)
            {
                Socket?.Close();
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
                while (bytes[i] == 0) i--;
                if (i >= 3 && bytes[i] == 84 && bytes[i - 1] == 79 && bytes[i - 2] == 69 && bytes[i - 3] == 92)
                {
                    bytes[i - 3] = 0;
                }
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
                if (Servers.TryGetValue(senderId, out server)) continue;
                server = new Server(ip.ToString(), port);
                Servers.TryAdd(senderId, server);
                OnNewServerResponse(server);
            }
        }

        #endregion Internal Methods

        #endregion Methods
    }
}
