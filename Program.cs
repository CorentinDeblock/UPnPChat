using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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
                    if(type != null)
                    {
                        if(type == "1")
                        {
                            StartClient(int.Parse(port));
                        } else if(type == "2")
                        {
                            StartServer(int.Parse(port));
                        }
                    }
                }

                Console.WriteLine("Press any keys to continue...");
                Console.ReadKey();
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void StartServer(int port)
        {
            SocketConnection listener = new SocketConnection(port);
            
            listener.socket.Bind(listener.endPoint);
            listener.socket.Listen(10);

            Console.WriteLine("Waiting for new connection");

            Socket handler = listener.socket.Accept();

            string data = null;
            byte[] bytes = null;

            while(true)
            {
                bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                if(data.IndexOf("<EOF>") > -1)
                {
                    break;
                }
            }

            Console.WriteLine($"Text received : {data}");

            byte[] msg = Encoding.ASCII.GetBytes(data);

            handler.Send(msg);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

        public static void StartClient(int port)
        {
            byte[] bytes = new byte[1024];
            SocketConnection connection = new SocketConnection(port);
            
            connection.socket.Connect(connection.endPoint);
            Console.WriteLine($"Socket connected to {connection.socket.RemoteEndPoint}");

            byte[] message = Encoding.ASCII.GetBytes("This is a test<EOF>");

            int bytesSent = connection.socket.Send(message);

            int bytesRec = connection.socket.Receive(bytes);
            Console.WriteLine($"Echoed test = {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");

            // Release the socket.
            connection.socket.Shutdown(SocketShutdown.Both);
            connection.socket.Close();
        }
    }
}
