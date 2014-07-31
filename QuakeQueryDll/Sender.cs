﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QuakeQueryDll
{
    internal class Sender
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly string _message;
        private readonly UdpClient _socket;
        private readonly QuakeQuery _communicator;

        internal Sender(QuakeQuery communicator, UdpClient socket, string ip, int port, string message) : this(communicator, socket, ip, (ushort)port, message) { }
        internal Sender(QuakeQuery communicator, UdpClient socket, string ip, ushort port, string message)
        {
            _ip = ip;
            _port = port;
            _message = message;
            _socket = socket;
            _communicator = communicator;
        }

        // Quake message header
        private readonly byte[] prefix = new byte[] { 255, 255, 255, 255 };

        internal void Send()
        {
            IPAddress destination_address = IPAddress.Parse(_ip); 
            IPEndPoint destination = new IPEndPoint(destination_address, _port);

            //Add header.
            byte[] bytes = Encoding.UTF8.GetBytes(_message);
            Array.Resize(ref bytes, bytes.Length + 4);
            Buffer.BlockCopy(bytes, 0, bytes, 4, bytes.Length - 4);
            Buffer.BlockCopy(prefix, 0, bytes, 0, 4);

            _socket.Send(bytes, bytes.Length, destination);

            string senderId = _ip + ":" + _port.ToString();
            Server server;
            if (!_communicator.Servers.TryGetValue(senderId, out server))
            {
                server = new Server(_ip, _port);
                _communicator.Servers.TryAdd(senderId, server);
                _communicator.OnNewServerResponse(server);
            }
            server.LastSendTime = DateTime.Now;
        }
    }
}
