using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UPnPChat.src
{
    public class Client<DataHandler, Data> : SocketConnection<Data>
        where Data : class
        where DataHandler : DataHandler<Data>, new()
    {
        private bool _quit = false;
        private string _ip = "";
        private int _port = 0;

        /// <summary>
        /// The host ip that you are connected to. Empty if you are not connected to any host
        /// </summary>
        public string Ip { get { return _ip; } }

        /// <summary>
        /// The host port that you are connected to. 0 if you are not connected to any host
        /// </summary>
        public int Port { get { return _port; } }

        public Action<string, int>? OnConnection { get { return __OnConnection; } set { __OnConnection = value; } }
        public Action<string, int>? OnConnected { get { return __OnConnected; } set { __OnConnected = value; } }
        public Action<Exception>? OnConnectionFailed { get { return __OnConnectionFailed; } set { __OnConnectionFailed = value; } }

        public Client() : base(new DataHandler(), Dns.GetHostEntry("localhost").AddressList[0], 0)
        {
            __OnConnection += (ip, port) =>
            {
                _ip = ip;
                _port = port;
            };

            OnConnectionClose += () => _quit = true;
        }

        public void Connect(string ip, int port)
        {
            __Connect(ip, port);
        }
        public void Connect(IPAddress ip, int port)
        {
            __Connect(ip, port);
        }

        public async Task ConnectAsync(string ip, int port)
        {
            await __ConnectAsync(ip, port);
        }

        public async Task ConnectAsync(IPAddress ip, int port)
        {
            await __ConnectAsync(ip, port);
        }

        public void Send(Data data)
        {
            __Send(Socket, data);
        }

        public async Task SendAsync(Data data)
        {
            await __SendAsync(Socket, data);
        }

        public void Receive()
        {
            while (_quit)
            {
                __Receive(Socket);
            }
        }

        public void ReceiveAsync()
        {
            _ = _ReceiveAsync();
        }

        public async Task _ReceiveAsync()
        {
            while (!_quit)
            {
                await __ReceiveAsync(Socket);
            }
        }
    }
}
