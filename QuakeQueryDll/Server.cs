using System;
using System.Collections.Generic;
using System.Net;

namespace QuakeQueryDll
{
    /// <summary>
    /// Contains IPAddress and Port
    /// </summary>
    //[Serializable()]
    public class Server
    {
        public readonly string Ip;
        public readonly ushort Port;

        public string Response { get { return _response; } internal set { _response = value; } }
        public DateTime LastSendTime { get; internal set; }
        public DateTime LastRecvTime { get; internal set; }
        public Dictionary<string, string> Info { get { return _info; } internal set { _info = value; } }
        public Dictionary<string, string> Status { get { return _status; } internal set { _status = value; } }

        private string _response = string.Empty;
        private Dictionary<string, string> _info = new Dictionary<string, string>();
        private Dictionary<string, string> _status = new Dictionary<string, string>();

        public Server(IPEndPoint ep)
        {
            LastRecvTime = new DateTime();
            LastSendTime = new DateTime();
            Ip = ep.Address.MapToIPv4().ToString();
            Port = (ushort)ep.Port;
        }
        public Server(string ip, int port)
        {
            LastRecvTime = new DateTime();
            LastSendTime = new DateTime();
            Ip = ip;
            Port = (ushort)port;
        }
        public Server(IPAddress ip, int port)
        {
            LastRecvTime = new DateTime();
            LastSendTime = new DateTime();
            Ip = ip.MapToIPv4().ToString();
            Port = (ushort)port;
        }

        public new string ToString()
        {
            return Ip + ":" + Port;
        }

        internal void UpdateInfo(string data)
        {
            var token = data.Split('\\');
            for (var i = 1; i < token.Length; i++)
                Info[token[i]] = token[++i];
        }
        internal void UpdateStatus(string data)
        {
            var line = data.Split('\n');
            var token = line[1].Split('\\');
            for (var i = 1; i < token.Length; i++)
                Status[token[i]] = token[++i];

            var players = "";
            for (var i = 2; i < line.Length; i++)
            {
                players += Environment.NewLine;
                players += line[i];
            }
            Status["Players"] = players.TrimEnd();
        }
    }
}