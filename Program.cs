﻿using Microsoft.VisualBasic;
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
        public static volatile bool Quit = false;
        public static volatile bool ClientDisconnect = false;

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


                    if(!ClientDisconnect)
                    {
                        // Release the socket.
                        connection.socket.Shutdown(SocketShutdown.Both);
                        connection.socket.Close();
                    }
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Interaction(Socket socket)
        {
            while (!Quit)
            {
                string? data = Console.ReadLine();

                if (data != null && !Quit)
                {
                    if (data == "/q")
                    {
                        Quit = true;
                    } else
                    {
                        socket.Send(Encoding.UTF8.GetBytes(data));
                    }
                }
            }
        }

        private static async Task Receive(Socket connection)
        {
            while (!Quit)
            {
                try
                {
                    byte[] bytes = new byte[1024];

                    int bytesRec = await connection.ReceiveAsync(bytes);

                    if (bytesRec != 0)
                    {
                        if (bytes[0] != 0x74)
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Client disconnect... Press any key to exit");
                    Quit = true;
                    ClientDisconnect = true;
                }
            }
        }

        private static void StartServer(SocketConnection listener)
        {
            listener.socket.Bind(listener.endPoint);
            listener.socket.Listen(10);

            Console.WriteLine("Waiting for new connection");

            Socket handler = listener.socket.Accept();

            Console.WriteLine("A client has connect");

            _ = Receive(handler);

            Interaction(handler);
        }

        private static async Task HearthBeat(Socket socket)
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            while (await periodicTimer.WaitForNextTickAsync())
            {
                socket.Send([0x74]);
            }
        }

        public static void StartClient(SocketConnection connection)
        {
            connection.socket.Connect(connection.endPoint);
            Console.WriteLine($"Socket connected to {connection.socket.RemoteEndPoint}");

            _ = Receive(connection.socket);

            Interaction(connection.socket);
        }
    }
}
