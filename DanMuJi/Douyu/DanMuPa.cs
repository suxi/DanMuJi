using System;
using System.Collections.Generic;
using System.Text;

namespace DanMuJi.Douyu
{
    class DanMuPa
    {
        public string ParsePackge(byte[] data)
        {
            var header = new byte[8];
            Array.Copy(data, header, 8);
            var body = new byte[data.Length - 8 - 1];
            Array.Copy(data, 8, body, 0, body.Length);

            return Encoding.UTF8.GetString(body);
        }
    }
}
