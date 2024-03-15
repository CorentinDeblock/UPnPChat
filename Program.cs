using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using UPnPChat.src;

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

    public class Message
    {
        public string Data;
        public DateTime Date;
        public string Author;

        public Message(string data, DateTime date, string author)
        {
            Data = data;
            Date = date;
            Author = author;
        }
    }

    class MessageDataHandler : DataHandler<Message>
    {
        public byte[] byteStorage()
        {
            return new byte[4096];
        }

        public Message Receive(byte[] data, int numBytes)
        {
            Message? json = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(data));
            
            if(json != null)
            {
                return json;
            }

            Console.WriteLine("Data was null");

            throw new Exception("Received invalid data");
        }

        public byte[] Send(Message data)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        }
    }

    internal class Program
    {
        private static volatile bool Quit = false;
        private static string Author = "";

        static void Main(string[] args)
        {
            string port = AskForValidData("Please enter port", "Please enter a valid port");
            string username = AskForValidData("Please enter a username", "Please enter a valid username");
            string type = AskForValidData("Enter connection type\n1. client\n2. server", "Please enter a valid connection type");

            Author = username;

            try
            {
                if (type == "1")
                {
                    var client = new Client<MessageDataHandler, Message>();

                    client.OnReceiveSucceeded += HandleSocketData;

                    client.OnConnection = (ip, port) => { Console.WriteLine($"Connecting to {ip}:{port}"); };
                    client.OnConnected = (ip, port) => { Console.WriteLine($"Connected to {ip}:{port}"); };
                    client.OnSocketDisconnected = (socket) => {
                        Console.WriteLine("Server has closed... You can type /q to quit");
                        client.Close();
                    };

                    client.Connect(Dns.GetHostEntry("localhost").AddressList[0], int.Parse(port));
                    client.ReceiveAsync();

                    Interaction(client);

                    client.Close();
                }
                else if (type == "2")
                {
                    var host = new Host<MessageDataHandler, Message>(Dns.GetHostEntry("localhost").AddressList[0], int.Parse(port));

                    host.OnListening += () => Console.WriteLine("Listening for connection... You can type /q to quit");
                    host.OnFailedToListen += (ex) => Console.WriteLine($"Failed to listen : {ex.Message} {host.Socket}");

                    host.OnClientConnected += (socket) => Console.WriteLine($"A client has connected");
                    host.OnSocketDisconnected += (socket) =>
                    {
                        Console.WriteLine("A client has disconnected");
                        if (host.SocketConnected.Count == 0)
                        {
                            Console.WriteLine("Alone in the lobby... Wait for connection to communicate or /q to quit");
                        }
                    };

                    host.OnReceiveSucceeded += HandleSocketData;

                    host.ListenAsync();
                    Interaction(host);

                    host.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void HandleSocketData(SocketData<Message> socketData)
        {
            Console.WriteLine($"[{socketData.data.Date.ToString("dd/MM/yyyy HH:mm:ss")}] {socketData.data.Author} : {socketData.data.Data}");
        }

        private static string AskForValidData(string welcomeMessage, string warnerMessage)
        {
            Console.WriteLine(welcomeMessage);
            string? data = Console.ReadLine();

            while (data == null ? true : data.Trim().Length == 0)
            {
                Console.WriteLine(warnerMessage);
                data = Console.ReadLine();
            }

            return data;
        }

        private static void Interaction(Host<MessageDataHandler, Message> host)
        {
            while (!Quit)
            {
                string? data = Console.ReadLine();

                if (data != null && !Quit)
                {
                    if (data == "/q")
                    {
                        Quit = true;
                    }
                    else
                    {
                        host.SendToAll(CreateMessageFromString(data));
                    }
                }
            }
        }

        private static void Interaction(Client<MessageDataHandler, Message> client)
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
                        client.Send(CreateMessageFromString(data));
                    }
                }
            }
        }

        private static Message CreateMessageFromString(string data)
        {
            return new Message(data, DateTime.Now, Author);
        }
    }
}
