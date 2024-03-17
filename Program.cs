using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UPnPChat.src;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    public struct Message
    {
        public string Content;
        public DateTime Date;
        public string Author;

        public Message(string content, DateTime date, string author)
        {
            Content = content;
            Date = date;
            Author = author;
        }
    }

    public class MessageRPC : RPC
    {
        public MessageRPC(Host host) : base(host)
        {
        }

        public MessageRPC(Client client) : base(client)
        {
        }

        [ServerRPC]
        public void ServerMessageRPC(Message message)
        {
            HandleMessage(
                message,
                (username) => CallBack(PrivateMessageFailed, Program.CreateMessageFromString($"Failed to send message to user {username}. User dosen't exists")),
                true
            );
        }

        private void HandleMessage(Message message, Action<string> OnMessageFailed, bool displayMessage)
        {
            if (message.Content.Length > 0)
            {
                if (message.Content.StartsWith("/to"))
                {
                    string username = message.Content.Split(" ")[1];

                    var socketDataCache =
                        SocketServerStorage
                        .SocketDataCache
                        .Where((socketData) => socketData.Value.Username == username).ToList();

                    string contentFormated = message.Content.Remove(0, $"/to {username} ".Length);

                    if (socketDataCache.Count > 0)
                    {
                        if (socketDataCache[0].Key == Socket)
                        {
                            DisplayMessage(Program.CreateMessageFromString(contentFormated));
                        }
                        else
                        {
                            CallTo(socketDataCache[0].Key, ClientMessageRPC, Program.CreateMessageFromString(contentFormated));
                        }
                    }
                    else
                    {
                        OnMessageFailed(username);
                    }
                }
                else
                {
                    if(displayMessage)
                    {
                        DisplayMessage(message);
                    }

                    Call(ClientMessageRPC, message);
                }
            }
        }

        [ClientRPC]
        public void PrivateMessageFailed(Message message)
        {
            Console.WriteLine(message.Content);
        }

        [ClientRPC]
        public void ClientMessageRPC(Message message)
        {
            DisplayMessage(message);
        }

        private void DisplayMessage(Message message)
        {
            Console.WriteLine($"[{message.Date.ToString("dd/MM/yyyy HH:mm:ss")}] {message.Author} : {message.Content}");
        }

        public void Send(Message message)
        {
            if(__networkType == NetworkType.Client)
            {
                Call(ServerMessageRPC, message);
            } else if(__networkType == NetworkType.Server) 
            {
                HandleMessage(message, (username) => Console.WriteLine("Failed to send message to user {username}. User dosen't exists"), false);
            }
        }
    }
    public struct SocketDataCache
    {
        public string Username;
        public Guid SocketId;
    }

    static class SocketServerStorage
    {
        public static Dictionary<Socket, SocketDataCache> SocketDataCache = new Dictionary<Socket, SocketDataCache>();
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
                    var client = new Client();

                    client.OnConnection = (ip, port) => { Console.WriteLine($"Connecting to {ip}:{port}"); };
                    client.OnConnected = (ip, port) => 
                    {
                        Console.WriteLine($"Connected to {ip}:{port}");
                        client.Send(new SocketDataCache
                        {
                            Username = username,
                            SocketId = client.SocketId
                        });
                    };

                    client.OnSocketDisconnected = (socket) =>
                    {
                        Console.WriteLine("Server has closed... You can type /q to quit");
                        client.Close();
                    };

                    client.Connect(Dns.GetHostEntry("localhost").AddressList[0], int.Parse(port));
                    client.ReceiveAsync();

                    Interaction(new MessageRPC(client));

                    client.Close();
                }
                else if (type == "2")
                {
                    var host = new Host(Dns.GetHostEntry("localhost").AddressList[0], int.Parse(port));
                    
                    SocketServerStorage.SocketDataCache.Add(host.Socket, new SocketDataCache
                    {
                        SocketId = host.SocketId,
                        Username = username,
                    });

                    host.AddDataHandler<SocketDataCache>((socketData, _) =>
                    {
                        SocketServerStorage.SocketDataCache.Add(socketData.Socket, socketData.DeserializeData<SocketDataCache>());
                    });

                    host.OnListening += () => Console.WriteLine("Listening for connection... You can type /q to quit");
                    host.OnFailedToListen += (ex) => Console.WriteLine($"Failed to listen : {ex.Message} {host.Socket}");

                    host.OnClientConnected += (socket) =>
                    {
                        Console.WriteLine($"A client has connected");
                    };

                    host.OnSocketDisconnected += (socket) =>
                    {
                        Console.WriteLine("A client has disconnected");
                        SocketServerStorage.SocketDataCache.Remove(socket);
                        if (host.SocketConnected.Count == 0)
                        {
                            Console.WriteLine("Alone in the lobby... Wait for connection to communicate or /q to quit");
                        }
                    };

                    host.ListenAsync();
                    Interaction(new MessageRPC(host));

                    host.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static string AskForValidData(string welcomeMessage, string warnMessage)
        {
            Console.WriteLine(welcomeMessage);
            string? data = Console.ReadLine();

            while (data == null ? true : data.Trim().Length == 0)
            {
                Console.WriteLine(warnMessage);
                data = Console.ReadLine();
            }

            return data;
        }

        private static void Interaction(MessageRPC rpc)
        {
            while (!Quit)
            {
                string? data = Console.ReadLine();

                if (data != null && !Quit && data.Trim().Length > 0)
                {
                    if (data == "/q")
                    {
                        Quit = true;
                    }
                    else
                    {
                        rpc.Send(CreateMessageFromString(data));
                    }
                }
            }
        }

        public static Message CreateMessageFromString(string data)
        {
            return new Message(data, DateTime.Now, Author);
        }
    }
}
