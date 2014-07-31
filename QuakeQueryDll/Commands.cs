using System;
using System.Net;
using System.Threading;

namespace QuakeQueryDll
{
    public partial class QuakeQuery
    {

        public void GetInfo(string ip, int port)
        {
            Send("getinfo", ip, port);
        }
        public void GetInfo(IPAddress ip, int port)
        {
            Send("getinfo", ip, port);
        }
        public void GetInfo(IPEndPoint server)
        {
            Send("getinfo", server);
        }

        public void GetStatus(string ip, int port)
        {
            Send("getstatus", ip, port);
        }
        public void GetStatus(IPAddress ip, int port)
        {
            Send("getstatus", ip, port);
        }
        public void GetStatus(IPEndPoint server)
        {
            Send("getstatus", server);
        }

        public void Send(string message, string ip, int port)
        {
            new Thread(new ThreadStart(new Sender(this, Socket, ip, port, message).Send)).Start();
        }
        public void Send(string message, IPAddress ip, int port)
        {
            Send(message, ip.ToString(), port);
        }
        public void Send(string message, IPEndPoint server)
        {
            Send(message, server.Address, server.Port);
        }

        public void Rcon(string rcon, string cmd, string ip, int port)
        {
            Send("rcon " + rcon + " " + cmd, ip, port);
        }
        public void Rcon(string rcon, string cmd, IPAddress ip, int port)
        {
            Send("rcon " + rcon + " " + cmd, ip, port);
        }
        public void Rcon(string rcon, string cmd, IPEndPoint server)
        {
            Send("rcon " + rcon + " " + cmd, server);
        }

        public void Print(string rcon, string text, string ip, int port)
        {
            Rcon(rcon, "\"" + text + "\"", ip, port);
        }
        public void Print(string rcon, string text, IPAddress ip, int port)
        {
            Rcon(rcon, "\"" + text + "\"", ip, port);
        }
        public void Print(string rcon, string text, IPEndPoint server)
        {
            Rcon(rcon, "\"" + text + "\"", server);
        }

        public void Say(string rcon, string text, string ip, int port)
        {
            Rcon(rcon, "say \"" + text + "\"", ip, port);
        }
        public void Say(string rcon, string text, IPAddress ip, int port)
        {
            Rcon(rcon, "say \"" + text + "\"", ip, port);
        }
        public void Say(string rcon, string text, IPEndPoint server)
        {
            Rcon(rcon, "say \"" + text + "\"", server);
        }

        public void BigText(string rcon, string text, string ip, int port)
        {
            Rcon(rcon, "bigtext \"" + text + "\"", ip, port);
        }
        public void BigText(string rcon, string text, IPAddress ip, int port)
        {
            Rcon(rcon, "bigtext \"" + text + "\"", ip, port);
        }
        public void BigText(string rcon, string text, IPEndPoint server)
        {
            Rcon(rcon, "bigtext \"" + text + "\"", server);
        }

        public void PM(string rcon, int id, string text, string ip, int port)
        {
            Rcon(rcon, "tell " + id + " \"" + text + "\"", ip, port);
        }
        public void PM(string rcon, int id, string text, IPAddress ip, int port)
        {
            Rcon(rcon, "tell " + id + " \"" + text + "\"", ip, port);
        }
        public void PM(string rcon, int id, string text, IPEndPoint server)
        {
            Rcon(rcon, "tell " + id + " \"" + text + "\"", server);
        }
        
        public void PM(string rcon, string id, string text, string ip, int port)
        {
            int playerID;
            if (id.Length > 0)
            {
                if (!Int32.TryParse(id, out playerID))
                    throw new Exception("ID is not in the correct format.");
            }
            else
            {
                throw new Exception("ID is not set.");
            }
            PM(rcon, playerID, text, ip, port);
        }
        public void PM(string rcon, string id, string text, IPAddress ip, int port)
        {
            PM(rcon, id, text, ip.ToString(), port);
        }
        public void PM(string rcon, string id, string text, IPEndPoint server)
        {
            PM(rcon, id, text, server.Address, server.Port);
        }

        public void Master(IPEndPoint server, int protocol, bool full, bool empty)
        {
            Master(server.Address, server.Port, protocol, full, empty);
        }
        public void Master(IPAddress ip, int port, int protocol, bool full, bool empty)
        {
            Master(ip.ToString(), port, protocol, full, empty);
        }
        public void Master(string ip, int port, int protocol, bool full, bool empty)
        {
            string extra = String.Empty;
            if (full)
                extra += " full";
            if (empty)
                extra += " empty";
            Send("getservers " + protocol.ToString() + extra, ip, port);
        }

        // WiP
        public static string GetCvar(string cvar, string text)
        {
            cvar = cvar.Replace(" ", "");
            cvar = cvar.Replace("\t", "");
            string RconOutput = text;
            var pattern = "\".+\"\\s+is:\"(.*)\\^7\"\\s+default:.*";
            var tmp = System.Text.RegularExpressions.Regex.Match(RconOutput, pattern);
            if (!tmp.Success)
            {
                pattern = "\".+\"\\s+is:\"(.*)\\^7\"";
                tmp = System.Text.RegularExpressions.Regex.Match(RconOutput, pattern);
                if (tmp.Success)
                    return tmp.Groups[1].Value;
                return null;
            }
            return tmp.Groups[1].Value;
        }
    }
}
