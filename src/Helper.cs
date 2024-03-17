using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPnPChat.src
{
    public static class SocketDataExt
    {
        public static T DeserializeData<T>(this SocketData data)
        {
            return JsonConvert.DeserializeObject<T>(data.Content.Data.ToString()!)!;
        }
    }
}
