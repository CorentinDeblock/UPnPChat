using Microsoft.VisualBasic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace UPnPChat
{
    class SocketConnection
    {
        string hostName = "localhost";

        public int port;
        public IPAddress ipAddress;
        public IPEndPoint endPoint;
        public Socket socket;

        public SocketConnection(int port)
        {
            this.port = port;

            IPHostEntry host = Dns.GetHostEntry(hostName);

            ipAddress = host.AddressList[0];
            endPoint = new IPEndPoint(ipAddress, port);
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter port");
            string? port = Console.ReadLine();

            Console.WriteLine("Enter connection type\n1. client\n2. server");
            string? type = Console.ReadLine();

            try
            {
                if(port != null)
                {
                    SocketConnection connection = new SocketConnection(int.Parse(port));

                    if(type != null)
                    {
                        if(type == "1")
                        {
                            StartClient(connection);
                        } else if(type == "2")
                        {
                            StartServer(connection);
                        }
                    }

                    // Release the socket.
                    connection.socket.Shutdown(SocketShutdown.Both);
                    connection.socket.Close();
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Interaction(Socket socket)
        {
            while (true)
            {
                string? data = Console.ReadLine();
                if (data != null)
                {
                    if (data == "/q")
                    {
                        break;
                    }

                    int bytesSent = socket.Send(Encoding.UTF8.GetBytes(data));
                }
            }
        }

        private static async Task Receive(Socket connection)
        {
            while (true)
            {
                byte[] bytes = new byte[1024];
                int bytesRec = await connection.ReceiveAsync(bytes);

                Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));
            }
        }

        private static void StartServer(SocketConnection listener)
        {
            listener.socket.Bind(listener.endPoint);
            listener.socket.Listen(10);

            Console.WriteLine("Waiting for new connection");

            Socket handler = listener.socket.Accept();

            Console.WriteLine("A client has connect");

            Task.Run(() => Receive(handler));

            Interaction(handler);
        }

        public static void StartClient(SocketConnection connection)
        {
            connection.socket.Connect(connection.endPoint);
            Console.WriteLine($"Socket connected to {connection.socket.RemoteEndPoint}");

            Task.Run(() => Receive(connection.socket));

            Interaction(connection.socket);
        }
    }
}
