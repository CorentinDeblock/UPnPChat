using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UPnPChat.src
{
    public interface DataHandler<Data> where Data : class
    {
        public byte[] Send(Data data);
        public Data Receive(byte[] data, int numBytes);
        public byte[] byteStorage();
    }

    public struct SocketData<Data>
    {
        public Data data;
        public byte[] bytes;
        public int numBytes;
    }


    // Should handle Exception better in the future. But it's ok for now
    public abstract class SocketConnection<Data> where Data : class
    {
        private DataHandler<Data> _dataHandler;
        private IPAddress _ipAddress;
        private IPEndPoint _endPoint;
        private Socket _socket;
        private bool _disposed;
        private bool _isBinded = false;

        // Create Getter and/or setter in final class for them
        // Host
        protected Action<Socket>? __OnClientConnected;

        protected Action? __OnListening;
        protected Action<Exception>? __OnFailedToListen;

        protected int __Backlog = 10;

        // Client
        protected Action<string, int>? __OnConnection;
        protected Action<string, int>? __OnConnected;
        protected Action<Exception>? __OnConnectionFailed;
        // ---------------------------------------------------

        public Action<SocketData<Data>>? OnSendSucceeded;
        public Action<Socket, Exception>? OnSendFailed;

        /// <summary>
        /// Call when a either a client socket is disconnected of the host or the host is disconnected
        /// The socket argument is either the client socket (if you are the host)
        /// Or your own socket if you are the client
        /// </summary>
        public Action<Socket>? OnSocketDisconnected;

        public Action<SocketData<Data>>? OnReceiveSucceeded;
        public Action<Socket, Exception>? OnReceiveFailed;

        public Action? OnConnectionClose;

        public Socket Socket { get { return _socket; } protected set { _socket = value; } }
        public IPAddress IPAddress { get { return _ipAddress; } protected set { _ipAddress = value; } }
        public IPEndPoint IPEndPoint { get { return _endPoint; } protected set { _endPoint = value; } }

        public SocketConnection(DataHandler<Data> dataHandler, IPAddress ip, int port)
        {
            _disposed = false;
            _dataHandler = dataHandler;
            _ipAddress = ip;
            _endPoint = new IPEndPoint(_ipAddress, port);
            _socket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        private void _Bind()
        {
            if (!_isBinded)
            {
                _disposed = false;
                _socket.Bind(_endPoint);
                _socket.Listen(__Backlog);

                if (__OnListening != null)
                {
                    __OnListening();
                }

                _isBinded = true;
            }
        }

        protected Socket? __Listen()
        {
            try
            {
                _Bind();

                Socket handler = _socket.Accept();

                if (__OnClientConnected != null)
                {
                    __OnClientConnected(handler);
                }

                return handler;
            }
            catch (Exception ex)
            {
                if (__OnFailedToListen != null)
                {
                    __OnFailedToListen(ex);
                }

                return null;
            }
        }

        protected async Task<Socket?> __ListenAsync()
        {
            try
            {
                _Bind();

                Socket handler = await _socket.AcceptAsync();

                if (__OnClientConnected != null)
                {
                    __OnClientConnected(handler);
                }

                return handler;
            } catch (Exception ex) 
            {
                if(__OnFailedToListen != null)
                {
                    __OnFailedToListen(ex);
                }

                return null;
            }
        }

        protected void __Connect(string ip, int port)
        {
            try
            {
                if (__OnConnection != null)
                {
                    __OnConnection(ip, port);
                }

                _socket.Connect(ip, port);
                _disposed = false;

                if (__OnConnected != null)
                {
                    __OnConnected(ip, port);
                }
            }catch (Exception ex) 
            {
                if(__OnConnectionFailed != null)
                {
                    __OnConnectionFailed(ex);
                }
            }
        }
        protected void __Connect(IPAddress ip, int port)
        {
            try
            {
                if (__OnConnection != null)
                {
                    __OnConnection(ip.ToString(), port);
                }

                _socket.Connect(ip, port);
                _disposed = false;

                if (__OnConnected != null)
                {
                    __OnConnected(ip.ToString(), port);
                }
            }
            catch (Exception ex)
            {
                if (__OnConnectionFailed != null)
                {
                    __OnConnectionFailed(ex);
                }
            }
        }

        protected async Task __ConnectAsync(string ip, int port)
        {
            try
            {
                if (__OnConnection != null)
                {
                    __OnConnection(ip, port);
                }

                await _socket.ConnectAsync(ip, port);
                _disposed = false;

                if (__OnConnected != null)
                {
                    __OnConnected(ip, port);
                }
            }
            catch (Exception ex)
            {
                if (__OnConnectionFailed != null)
                {
                    __OnConnectionFailed(ex);
                }
            }
        }
        protected async Task __ConnectAsync(IPAddress ip, int port)
        {
            try
            {
                if (__OnConnection != null)
                {
                    __OnConnection(ip.ToString(), port);
                }

                await _socket.ConnectAsync(ip, port);
                _disposed = false;

                if (__OnConnected != null)
                {
                    __OnConnected(ip.ToString(), port);
                }
            }
            catch (Exception ex)
            {
                if (__OnConnectionFailed != null)
                {
                    __OnConnectionFailed(ex);
                }
            }
        }

        protected void __Send(Socket socket, Data data)
        {
            try
            {
                byte[] bytesSend = _dataHandler.Send(data);
                int numBytesSend = socket.Send(bytesSend);

                if(OnSendSucceeded != null)
                {
                    OnSendSucceeded(new SocketData<Data>{ data = data, bytes = bytesSend, numBytes = numBytesSend });
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
                    _disposed = true;
                }
            }
            catch (SocketException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
                }

                if (OnSocketDisconnected != null)
                {
                    OnSocketDisconnected(socket);
                }
            }
        }

        protected async Task __SendAsync(Socket socket, Data data)
        {
            try
            {
                byte[] bytesSend = _dataHandler.Send(data);
                int numBytesSend = await socket.SendAsync(bytesSend);

                if (OnSendSucceeded != null)
                {
                    OnSendSucceeded(new SocketData<Data> { data = data, bytes = bytesSend, numBytes = numBytesSend });
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
                    _disposed = true;
                }
            }
            catch (SocketException ex)
            {
                if (OnSocketDisconnected != null)
                {
                    OnSocketDisconnected(socket);
                }

                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
                }
            }
        }

        protected Data? __Receive(Socket socket)
        {
            try
            {
                byte[] bytesReceive = _dataHandler.byteStorage();
                int numByteReceive = socket.Receive(bytesReceive);
                Data data = _dataHandler.Receive(bytesReceive, numByteReceive);

                if (OnReceiveSucceeded != null)
                {
                    OnReceiveSucceeded(new SocketData<Data> { data = data, bytes = bytesReceive, numBytes = numByteReceive });
                }

                return data;
            }
            catch (ObjectDisposedException ex)
            {
                if (OnReceiveFailed != null)
                {
                    OnReceiveFailed(socket, ex);
                    _disposed = true;
                }

                return null;
            }
            catch (SocketException ex)
            {
                if (OnSocketDisconnected != null)
                {
                    OnSocketDisconnected(socket);
                }

                if (OnReceiveFailed != null)
                {
                    OnReceiveFailed(socket, ex);
                }

                return null;
            }
        }

        protected async Task<Data?> __ReceiveAsync(Socket socket)
        {
            try
            {
                byte[] bytesReceive = _dataHandler.byteStorage();
                int numByteReceive = await socket.ReceiveAsync(bytesReceive);
                Data data = _dataHandler.Receive(bytesReceive, numByteReceive);

                if (OnReceiveSucceeded != null)
                {
                    OnReceiveSucceeded(new SocketData<Data> { data = data, bytes = bytesReceive, numBytes = numByteReceive });
                }

                return data;
            }
            catch (ObjectDisposedException ex)
            {
                if (OnReceiveFailed != null)
                {
                    OnReceiveFailed(socket, ex);
                    _disposed = true;
                }

                return null;
            }
            catch(SocketException ex)
            {
                if (OnSocketDisconnected != null)
                {
                    OnSocketDisconnected(socket);
                }

                if (OnReceiveFailed != null)
                {
                    OnReceiveFailed(socket, ex);
                }

                return null;
            }
        }

        public void Close()
        {
            if (!_disposed)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }

            if(OnConnectionClose != null && !_disposed)
            {
                OnConnectionClose();
            }

            _disposed = true;
        }
    }
}