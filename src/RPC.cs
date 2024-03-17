using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UPnPChat.src
{
    public enum NetworkType
    {
        Client,
        Server
    }

    internal class RPCFuncStorage
    {
        public MethodInfo method;
        public NetworkType networkType;

        public RPCFuncStorage(MethodInfo method, NetworkType networkType)
        {
            this.method = method;
            this.networkType = networkType;
        }
    }

    internal struct RPCSocketData
    {
        public string name;
        public object[] arguments;
    }

    public class RPC
    {
        private Dictionary<string, RPCFuncStorage> _RPCFunc = new Dictionary<string, RPCFuncStorage>();
        private Host? _host;
        private Client? _client;
        private bool _isOwner;

        protected Socket? __lastSocket;
        protected NetworkType __networkType;

        protected Socket Socket
        {
            get
            {
                if(_host != null)
                {
                    return _host.Socket;
                }

                return _client!.Socket;
            }
        }

        protected bool IsOwner
        {
            get { return _isOwner; }
        }

        public RPC(Host host)
        {
            Populate();

            _host = host;
            _host.OnReceiveSucceeded += (socket, _, _) => __lastSocket = socket;
            _host.AddDataHandler<RPCSocketData>(_RPCReceive);
            __networkType = NetworkType.Server;
        }

        public RPC(Client client)
        {
            Populate();

            _client = client;
            _client.AddDataHandler<RPCSocketData>(_RPCReceive);
            __networkType = NetworkType.Client;
        }

        private void Populate()
        {
            foreach (MethodInfo info in GetType().GetMethods())
            {
                if (info.GetCustomAttribute<ClientRPC>() != null)
                {
                    _RPCFunc.Add(info.Name, new RPCFuncStorage(info, NetworkType.Client));

                }
                else if (info.GetCustomAttribute<ServerRPC>() != null)
                {
                    _RPCFunc.Add(info.Name, new RPCFuncStorage(info, NetworkType.Server));
                }
            }
        }

        private void _RPCReceive(SocketData data, int _) 
        {
            try
            {
                if(Socket == data.Socket)
                {
                    _isOwner = true;
                }

                RPCSocketData RPCData = data.DeserializeData<RPCSocketData>();
                _CallFunc(RPCData.name, RPCData.arguments);
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void _CallFunc(string name, object[] parameter)
        {
            try
            {
                RPCFuncStorage rpc = _RPCFunc[name];

                List<object> parameters = new List<object>();
                ParameterInfo[] parameterInfos = rpc.method.GetParameters();

                for(int i = 0; i < parameter.Length; i++)
                {
                    parameters.Add(JsonConvert.DeserializeObject(parameter[i].ToString()!, parameterInfos[i].ParameterType)!);
                }
            
                rpc.method.Invoke(this, parameters.ToArray());
            } catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Send the call to a specific socket. The socket is determined with a predicate that will match with the socket data cache.
        /// This function does not work on a client instance
        /// </summary>
        /// <typeparam name="RPCType">The data type</typeparam>
        /// <param name="predicate">The predicate that will match with the socket data cache</param>
        /// <param name="RPCfunc">The data</param>
        /// <param name="parameter">The arguments of the function called</param>
        protected void CallTo<RPCType>(Socket socket, RPCType data, params object[] parameter) where RPCType : Delegate
        {
            RPCFuncStorage rpc = _RPCFunc[data.Method.Name];

            RPCSocketData socketData = new RPCSocketData
            {
                name = rpc.method.Name,
                arguments = parameter
            };

            if (_host != null)
            {
                _host.Send(socket, socketData);
            }
        }

        /// <summary>
        /// Call a RPC method
        /// </summary>
        /// <typeparam name="RPCType">Type of RPC func</typeparam>
        /// <param name="RPCfunc">The RPC func</param>
        /// <param name="parameter">The parameter</param>
        /// 
        protected void Call<RPCType>(RPCType RPCfunc, params object[] parameter) where RPCType : Delegate
        {
            RPCFuncStorage rpc = _RPCFunc[RPCfunc.Method.Name];
            RPCSocketData socketData = new RPCSocketData
            {
                name = rpc.method.Name,
                arguments = parameter
            };

            if (rpc.networkType == NetworkType.Server && _client != null)
            {
                _client.Send(socketData);
            } else if(rpc.networkType == NetworkType.Client && _host != null)
            {
                if (__lastSocket != null)
                {
                    _host.SendToAllExcept(socketData, [__lastSocket]);
                    __lastSocket = null;
                } else
                {
                    _host.SendToAll(socketData);
                }
            }
        }

        /// <summary>
        /// Call a RPC method to the last socket that has send a message. It's useful when you need to call a client error rpc function
        /// </summary>
        /// <typeparam name="RPCType">Type of RPC func</typeparam>
        /// <param name="RPCfunc">The RPC func</param>
        /// <param name="parameter">The parameter</param>
        protected void CallBack<RPCType>(RPCType RPCfunc, params object[] parameter) where RPCType : Delegate
        {
            CallTo(__lastSocket!, RPCfunc, parameter);
        }
    }

    public class ServerRPC : Attribute
    {

    }

    public class ClientRPC : Attribute
    {

    }
}
