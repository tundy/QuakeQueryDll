using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace QuakeQueryDll
{
    internal class Receiver
    {
        private readonly QuakeQuery _communicator;

        internal Receiver(QuakeQuery communicator)
        {
            _communicator = communicator;
        }

        internal void Loop()
        {
            // Here will be saved information about UDP sender.
            var sender = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                try
                {
                    // Here will be saved UDP data.
                    var bytes = _communicator.Socket.Receive(ref sender);
                    if (bytes.Length > 4 && bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255)   // Check header.
                    {
                        Analyze(bytes, sender);
                    }
                    // else Not a Quake datagam.
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        // An existing connection was forcibly closed by the remote host
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private void Analyze(byte[] bytes, IPEndPoint sender)
        {
            // Cut off header.
            var message = Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4);  

            // Identify Server
            var senderId = sender.Address + ":" + sender.Port;
            Server server;
            if (!_communicator.Servers.TryGetValue(senderId, out server))
            {
                server = new Server(sender);
                _communicator.Servers.TryAdd(senderId, server);
                _communicator.OnNewServerResponse(server);
            }

            // Analyze type of packet
            server.LastRecvTime = DateTime.Now;
            server.Response = message;

            if (message.StartsWith("infoResponse"))
            {
                // Analyze Info
                server.UpdateInfo(QuakeQuery.FixNewLines(message));
                _communicator.OnInfoResponse(server);
            }
            else if (message.StartsWith("statusResponse"))
            {
                // Analyze Status
                server.UpdateStatus(QuakeQuery.FixNewLines(message));
                _communicator.OnStatusResponse(server);
            }
            else if (message.StartsWith("print"))
            {
                // print
                var tmp = QuakeQuery.FixNewLines(message);
                tmp = tmp.Substring(tmp.IndexOf('\n') + 1);
                server.Response = tmp;
                server.Cvar = GetCvar(tmp);
                if (server.Cvar != null && server.Cvar.Success)
                {
                    if (!server.Cvars.ContainsKey(server.Cvar.Groups[1].Value))
                    {
                        server.Cvars.Add(server.Cvar.Groups[1].Value, server.Cvar.Groups[2].Value);
                    }
                    else
                    {
                        server.Cvars[server.Cvar.Groups[1].Value] = server.Cvar.Groups[2].Value;
                    }
                    _communicator.OnCvarSuccess(server);
                }
                _communicator.OnPrintResponse(server);
            }
            else if (message.StartsWith("getserversResponse"))
            {
                // server list
                _communicator.DecodeServerList(bytes);
                _communicator.OnMasterResponse(server);
            }
            else
            {
                _communicator.OnOtherResponse(server);
            }
            _communicator.OnServerResponse(server);
        }

        private static Match GetCvar(string rconOutput)
        {
            var pattern = "\"(.+)\"\\s+is:\"(.*)\\^7\"\\s+default:.*";
            var tmp = Regex.Match(rconOutput, pattern);
            if (tmp.Success) return tmp;
            pattern = "\"(.+)\"\\s+is:\"(.*)\\^7\"";
            tmp = Regex.Match(rconOutput, pattern);
            return tmp.Success ? tmp : null;
        }
    } // end of class UDPListener

}
