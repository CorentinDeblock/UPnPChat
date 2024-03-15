using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPnPChat.src
{
    class StringDataHandler : DataHandler<string>
    {
        public byte[] byteStorage()
        {
            return new byte[4096];
        }

        public string Receive(byte[] data, int numBytes)
        {
            return Encoding.UTF8.GetString(data, 0, numBytes);
        }

        public byte[] Send(string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }
    }

    class BinaryDataHandler : DataHandler<byte[]>
    {
        public byte[] byteStorage()
        {
            return new byte[4096];
        }

        public byte[] Receive(byte[] data, int numBytes)
        {
            return data;
        }

        public byte[] Send(byte[] data)
        {
            return data;
        }
    }
}
