using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QuakeQueryDll
{
    public class Receiver
    {
        private readonly QuakeQuery _communicator;
        internal bool work = true;

        public Receiver(QuakeQuery communicator)
        {
            _communicator = communicator;
        }

        public void Loop()
        {
            // Here will be saved information about UDP sender.
            var sender = new IPEndPoint(IPAddress.Any, 0);
            // Here will be saved UDP data.
            byte[] bytes;
            
            while (work)
            {
                try
                {
                    bytes = _communicator.Socket.Receive(ref sender);
                    if (bytes.Length > 4 && bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255)   // Check header.
                    {
                        Analyze(bytes, sender);
                    }
                    else
                    {
                        // Not a Quake datagam.
                    }
                }
                catch (ThreadAbortException)
                {
                    work = false;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        // An existing connection was forcibly closed by the remote host
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }

        private void Analyze(byte[] bytes, IPEndPoint sender)
        {
            // Cut off header.
            string message = Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4);  

            // Identify Server
            string senderId = sender.Address.ToString() + ":" + sender.Port.ToString();
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
                string tmp = QuakeQuery.FixNewLines(message);
                tmp = tmp.Substring(tmp.IndexOf('\n') + 1);
                server.Response = tmp;
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
    } // end of class UDPListener
}