using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace UPnPChat.src
{
    using DataHandler = Action<SocketData, int>;

    public struct SocketContent
    {
        public string Annotation;
        public object Data;
    }

    public struct SocketData
    {
        public SocketContent Content;
        public Socket Socket;
    }

    // Should handle Exception better in the future. But it's ok for now
    public abstract class SocketConnection
    {
        private IPAddress _ipAddress;
        private IPEndPoint _endPoint;
        private Socket _socket;
        private bool _isBinded = false;
        public Guid _socketId;

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

        public Action<byte[], int>? OnSendSucceeded;
        public Action<Socket, Exception>? OnSendFailed;

        /// <summary>
        /// Call when a either a client socket is disconnected of the host or the host is disconnected
        /// The socket argument is either the client socket (if you are the host)
        /// Or your own socket if you are the client
        /// </summary>
        public Action<Socket>? OnSocketDisconnected;

        public Action<Socket, byte[], int>? OnReceiveSucceeded;
        public Action<Socket, Exception>? OnReceiveFailed;
        public Action<Exception>? OnConnectionCloseFailed;

        public int MaximumSocketBufferSize = 8192;

        public Action? OnConnectionClose;

        public Dictionary<string, DataHandler> DataHandlers = new Dictionary<string, DataHandler>();

        public Socket Socket { get { return _socket; } protected set { _socket = value; } }
        public IPAddress IPAddress { get { return _ipAddress; } protected set { _ipAddress = value; } }
        public IPEndPoint IPEndPoint { get { return _endPoint; } protected set { _endPoint = value; } }
        public Guid SocketId { get { return _socketId; } }

        public SocketConnection(IPAddress ip, int port)
        {
            
            _ipAddress = ip;
            _endPoint = new IPEndPoint(_ipAddress, port);
            _socket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socketId = Guid.NewGuid();

            Console.WriteLine(_socketId);
        }

        private void _Bind()
        {
            if (!_isBinded)
            {
                
                _socket.Bind(_endPoint);
                _socket.Listen(__Backlog);

                if (__OnListening != null)
                {
                    __OnListening();
                }

                _isBinded = true;
            }
        }
        private byte[] _CreateObject<Data>(Data data) where Data : struct
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SocketContent
            {
                Data = data,
                Annotation = typeof(Data).Name
            }));
        }
        private SocketContent _GetSocketContent(byte[] data)
        {
            return JsonConvert.DeserializeObject<SocketContent>(Encoding.UTF8.GetString(data));
        }

        private SocketData _CreateSocketData(Socket socket, SocketContent content)
        {
            return new SocketData
            {
                Socket = socket,
                Content = content,
            };
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

        protected void __Send<Data>(Socket socket, Data data) where Data : struct
        {
            try
            {
                byte[] byteSend = _CreateObject(data);
                int numBytesSend = socket.Send(byteSend);

                if(OnSendSucceeded != null)
                {
                    OnSendSucceeded(byteSend, numBytesSend);
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
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

        protected async Task __SendAsync<Data>(Socket socket, Data data) where Data : struct
        {
            try
            {
                byte[] bytesSend = _CreateObject(data);
                int numBytesSend = await socket.SendAsync(bytesSend);

                if (OnSendSucceeded != null)
                {
                    OnSendSucceeded(bytesSend, numBytesSend);
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
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
        protected void __Send(Socket socket, byte[] data)
        {
            try
            {
                int numBytesSend = socket.Send(data);

                if (OnSendSucceeded != null)
                {
                    OnSendSucceeded(data, numBytesSend);
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
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

        protected async Task __SendAsync(Socket socket, byte[] data)
        {
            try
            {
                int numBytesSend = await socket.SendAsync(data);

                if (OnSendSucceeded != null)
                {
                    OnSendSucceeded(data, numBytesSend);
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (OnSendFailed != null)
                {
                    OnSendFailed(socket, ex);
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

        protected byte[] __Receive(Socket socket)
        {
            try
            {
                byte[] bytesReceive = new byte[MaximumSocketBufferSize];
                int numByteReceive = socket.Receive(bytesReceive);
                
                SocketContent socketContent = _GetSocketContent(bytesReceive);
                DataHandler handler = DataHandlers[socketContent.Annotation];

                if (OnReceiveSucceeded != null)
                {
                    OnReceiveSucceeded(socket, bytesReceive, numByteReceive);
                }

                if (handler != null)
                {
                    handler(_CreateSocketData(socket, socketContent), numByteReceive);
                }

                return bytesReceive;
            }
            catch (ObjectDisposedException ex)
            {
                if (OnReceiveFailed != null)
                {
                    OnReceiveFailed(socket, ex);
                }

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
            }
            return [];
        }

        protected async Task<byte[]> __ReceiveAsync(Socket socket)
        {
            try
            {
                byte[] bytesReceive = new byte[MaximumSocketBufferSize];
                int numByteReceive = await socket.ReceiveAsync(bytesReceive);

                SocketContent socketContent = _GetSocketContent(bytesReceive);
                DataHandler handler = DataHandlers[socketContent.Annotation];

                if (OnReceiveSucceeded != null)
                {
                    OnReceiveSucceeded(socket, bytesReceive, numByteReceive);
                }

                if (handler != null)
                {
                    handler(_CreateSocketData(socket, socketContent), numByteReceive);
                }

                return bytesReceive;
            }
            catch (ObjectDisposedException ex)
            {
                if (OnReceiveFailed != null)
                {
                    OnReceiveFailed(socket, ex);
                }
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
            }

            return [];
        }

        public void Close()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();

                if (OnConnectionClose != null)
                {
                    OnConnectionClose();
                }
            }catch (Exception ex)
            {
                if(OnConnectionCloseFailed != null)
                {
                    OnConnectionCloseFailed(ex);
                }
            }
        }

        public void AddDataHandler<Data>(DataHandler dataHandler) where Data : struct
        {
            DataHandlers.Add(typeof(Data).Name, dataHandler);
        }
    }
}