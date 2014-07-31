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
        public readonly string IP;
        public readonly ushort Port;

        public string Response { get { return _response; } internal set { _response = value; } }
        public DateTime LastSendTime { get { return _LastSendTime; } internal set { _LastSendTime = value; } }
        public DateTime LastRecvTime { get { return _LastRecvTime; } internal set { _LastRecvTime = value; } }
        public Dictionary<string, string> Info { get { return _info; } internal set { _info = value; } }
        public Dictionary<string, string> Status { get { return _status; } internal set { _status = value; } }

        private string _response = string.Empty;
        private DateTime _LastSendTime = new DateTime();
        private DateTime _LastRecvTime = new DateTime();
        private Dictionary<string, string> _info = new Dictionary<string, string>();
        private Dictionary<string, string> _status = new Dictionary<string, string>();

        public Server(IPEndPoint ep)
        {
            IP = ep.Address.MapToIPv4().ToString();
            Port = (ushort)ep.Port;
        }
        public Server(string ip, int port)
        {
            IP = ip;
            Port = (ushort)port;
        }
        public Server(IPAddress ip, int port)
        {
            IP = ip.MapToIPv4().ToString();
            Port = (ushort)port;
        }

        public new string ToString()
        {
            return IP.ToString() + ":" + Port.ToString();
        }

        internal void UpdateInfo(string data)
        {
            var token = data.Split('\\');
            for (int i = 1; i < token.Length; i++)
            {
                Info[token[i]] = token[++i];
            }
        }
        internal void UpdateStatus(string data)
        {
            var line = data.Split('\n');
            var token = line[1].Split('\\');
            for (int i = 1; i < token.Length; i++)
            {
                Status[token[i]] = token[++i];
            }

            string players = "";
            for (int i = 2; i < line.Length; i++)
            {
                players += Environment.NewLine;
                players += line[i];
            }
            Status["Players"] = players.TrimEnd();
        }
    }
}