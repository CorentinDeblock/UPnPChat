using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.IO;
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
    public class AesCbcCipher
    {
        public byte[] Iv { get; }
        public byte[] CiphertextBytes { get; }

        public static AesCbcCipher FromByte(byte[] data, int numBytes)
        {
            string str = Encoding.Unicode.GetString(data, 0, numBytes);
            byte[] original = Convert.FromBase64String(str);

            return new AesCbcCipher(
                original.Take(16).ToArray(),
                original.Skip(16).ToArray()
            );
        }

        public AesCbcCipher(byte[] iv, byte[] ciphertextBytes)
        {
            Iv = iv;
            CiphertextBytes = ciphertextBytes;
        }

        public byte[] ToBytes()
        {
            byte[] original = Iv.Concat(CiphertextBytes).ToArray();
            string base64 = Convert.ToBase64String(original);
            return Encoding.Unicode.GetBytes(base64);
        }
    }

    public class AESMiddleware : Middleware
    {
        public byte[] Receive(byte[] data, int numBytes)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    var aesCypher = AesCbcCipher.FromByte(data, numBytes);
                    var decryptor = aes.CreateDecryptor(_GetKey(), aesCypher.Iv);

                    using (MemoryStream memoryStream = new MemoryStream(aesCypher.CiphertextBytes))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            string strData;
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                strData = streamReader.ReadToEnd();
                            }

                            return Encoding.Unicode.GetBytes(strData);
                        }
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private byte[] _GetKey() => Encoding.Unicode.GetBytes(Program.Key);

        public byte[] Send(byte[] data)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    var encryptor = aes.CreateEncryptor(_GetKey(), aes.IV);
                    var descryptor = aes.CreateDecryptor(_GetKey(), aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            string datastr = Encoding.Unicode.GetString(data);

                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(datastr);
                            }
                        }

                        byte[] allDAta = new AesCbcCipher(aes.IV, memoryStream.ToArray()).ToBytes();

                        return allDAta;
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
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
            try
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
                            CallTo(socketDataCache[0].Key, ClientMessageRPC, Program.CreateMessageFromString(contentFormated));
                        }
                        else
                        {
                            Feedback(PrivateMessageFailed, Program.CreateMessageFromString($"Failed to send message to user \"{username}\". User dosen't exists"));
                        }
                    }
                    else
                    {
                        Call(ClientMessageRPC, message);
                    }
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex);
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
            try
            {
                Call(ServerMessageRPC, message);
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
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
        public static string Key = Environment.GetEnvironmentVariable("KEY")!;

        static void Main(string[] args)
        {
            DotNetEnv.Env.TraversePath().Load();

            string port = AskForValidData("Please enter port", "Please enter a valid port");
            string username = AskForValidData("Please enter a username", "Please enter a valid username");
            string type = AskForValidData("Enter connection type\n1. client\n2. server", "Please enter a valid connection type");

            Author = username;

            if (type != "1" && type != "2")
            {
                return;
            }

            try
            {
                var client = new Client();
                client.Middlewares.Add(new AESMiddleware());

                if (type == "2")
                {
                    var host = new Host(Dns.GetHostEntry("localhost").AddressList[0], int.Parse(port));
                    host.Middlewares.Add(new AESMiddleware());

                    new MessageRPC(host);

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
                        if (host.SocketConnected.Count == 1)
                        {
                            Console.WriteLine("Alone in the lobby... Wait for connection to communicate or /q to quit");
                        }
                    };

                    host.ListenAsync();

                    client.OnConnectionClose += () => host.Close();
                }

                client.OnConnected = (ip, port) =>
                {
                    if(type == "1")
                    {
                        Console.WriteLine($"Connected to {ip}:{port}");
                    }

                    client.Send(new SocketDataCache
                    {
                        Username = username,
                        SocketId = client.SocketId
                    });
                };

                if (type == "1")
                {
                    client.OnConnection += (ip, port) => { Console.WriteLine($"Connecting to {ip}:{port}"); };

                    client.OnSocketDisconnected += (socket) =>
                    {
                        Console.WriteLine("Server has closed... You can type /q to quit");
                        client.Close();
                    };

                }

                client.Connect(Dns.GetHostEntry("localhost").AddressList[0], int.Parse(port));
                client.ReceiveAsync();

                Interaction(new MessageRPC(client));

                client.Close();
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
