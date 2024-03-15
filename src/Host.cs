using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UPnPChat.src
{
    public class Host<DataHandler, Data> : SocketConnection<Data>
    where Data : class
    where DataHandler : DataHandler<Data>, new()
    {
        private bool _quit = false;
        private List<Socket> _socketConnected = new List<Socket>();

        public Host(IPAddress ip, int port) : base(new DataHandler(), ip, port)
        {
            OnConnectionClose += () => _quit = true;
            OnClientConnected += _socketConnected.Add;
            OnSocketDisconnected += (socket) => _socketConnected.Remove(socket);
        }

        public Action<Socket>? OnClientConnected { get { return __OnClientConnected; } set { __OnClientConnected = value; } }
        /// <summary>
        /// Return the list of socket (client) connected to this host
        /// </summary>
        public List<Socket> SocketConnected { get { return _socketConnected; } }
        public Action? OnListening { get { return __OnListening; } set { __OnListening = value; } }
        public Action<Exception>? OnFailedToListen { get { return __OnFailedToListen; } set { __OnFailedToListen = value; } }

        public int Backlog { get { return __Backlog; } set { __Backlog = value; } }

        public void Listen()
        {
            while (!_quit)
            {
                Socket? handler = __Listen();

                if (handler != null)
                {
                    while (_IsSocketConnected(handler))
                    {
                        __Receive(handler);
                    }
                }
            }
        }

        public void ListenAsync()
        {
            _ = _HostListenerAsync();
        }

        private bool _IsSocketConnected(Socket handler)
        {
            return _socketConnected.Contains(handler);
        }

        private async Task _HostListenerAsync()
        {
            while (!_quit)
            {
                Socket? handler = await __ListenAsync();

                if (handler != null)
                {
                    _ = _ReceiveAsync(handler);
                }
            }
        }

        private async Task _ReceiveAsync(Socket handler)
        {
            while (_IsSocketConnected(handler))
            {
                Data? data = await __ReceiveAsync(handler);

                if (data != null)
                {
                    SendToAllExcept(data, [handler]);
                }
            }
        }

        public void SendToAllExcept(Data data, List<Socket> socketIgnored)
        {
            foreach (Socket socket in _socketConnected)
            {
                if (!socketIgnored.Contains(socket))
                {
                    _ = SendAsync(socket, data);
                }
            }
        }

        public void SendToAll(Data data)
        {
            foreach (Socket socket in _socketConnected)
            {
                _ = SendAsync(socket, data);
            }
        }

        public void Send(Socket socket, Data data)
        {
            __Send(socket, data);
        }

        public async Task SendAsync(Socket socket, Data data)
        {
            await __SendAsync(socket, data);
        }
    }
}
