using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuakeQueryDll
{
    /// <summary>
    /// Contains IPAddress and Port
    /// </summary>
    [Serializable()]
    public class Server
    {
        public IPAddress IP;
        public ushort Port;

        public Server()
        {
        }
        public Server(string ip, int port)
        {
            IP = IPAddress.Parse(ip);
            Port = (ushort)port;
        }
        public Server(string ip, string port)
        {
            IP = IPAddress.Parse(ip);
            Port = (ushort)Int32.Parse(port);
        }
        public Server(string ip, ushort port)
        {
            IP = IPAddress.Parse(ip);
            Port = port;
        }
        public Server(IPAddress ip, ushort port)
        {
            IP = ip;
            Port = port;
        }
    }
}
