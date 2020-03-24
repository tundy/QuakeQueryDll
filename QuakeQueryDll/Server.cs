using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace QuakeQueryDll
{
    /// <summary>
    /// Contains IPAddress and Port
    /// </summary>
    //[Serializable()]
    public class Server
    {
        public readonly IPEndPoint IPEndPoint;
        public string IP => IPAddress.MapToIPv4().ToString();
        public int Port => IPEndPoint.Port;
        public IPAddress IPAddress => IPEndPoint.Address;

        public string Response { get; internal set; }
        public Match Cvar { get; internal set; }
        public DateTime LastSendTime { get; internal set; }
        public DateTime LastRecvTime { get; internal set; }
        public readonly Dictionary<string, string> Info = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Cvars = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Status = new Dictionary<string, string>();

        public Server(IPEndPoint ep)
        {
            LastRecvTime = new DateTime();
            LastSendTime = new DateTime();
            IPEndPoint = ep;
        }

        public Server(string ip, int port)
        {
            LastRecvTime = new DateTime();
            LastSendTime = new DateTime();
            IPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public Server(IPAddress ip, int port)
        {
            LastRecvTime = new DateTime();
            LastSendTime = new DateTime();
            IPEndPoint = new IPEndPoint(ip, port);
        }

        public override string ToString() => IP + ":" + Port;

        public override bool Equals(object obj)
        {
            var tmp = obj as Server;
            return tmp != null && ((IP == tmp.IP) && (Port == tmp.Port));
        }

        public override int GetHashCode()
        {
            var tmp = IP.Split('.');
            var hash = 0;
            hash += Convert.ToInt32(tmp[0]) << 24;
            hash += Convert.ToInt32(tmp[1]) << 16;
            hash += Convert.ToInt32(tmp[2]) << 8;
            hash += Convert.ToInt32(tmp[3]);
            return hash ^ Port;
        }

        public static bool operator ==(Server a, Server b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if ((a is null) || (b is null))
            {
                return false;
            }

            return a.IP == b.IP && a.Port == b.Port;
        }

        public static bool operator !=(Server a, Server b) => !(a == b);

        internal void UpdateInfo(string data)
        {
            var token = data.Split('\\');
            for (var i = 1; i < token.Length; i++)
            {
                Info[token[i]] = token[++i];
            }
        }

        internal void UpdateStatus(string data)
        {
            var line = data.Split('\n');
            var token = line[1].Split('\\');
            for (var i = 1; i < token.Length; i++)
            {
                Status[token[i]] = token[++i];
            }

            var players = string.Empty;
            for (var i = 2; i < line.Length; i++)
            {
                players += Environment.NewLine;
                players += line[i];
            }
            Status["Players"] = players.TrimEnd();
        }
    }
}