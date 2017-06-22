using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace DanMuJi.Bilibili
{
    public class DanMuPa
    {
        public enum MessageType
        {
            Live, Preparing,Danmu,Gift,Welcome,Dammy
        }

        public string ParsePackage(Byte[] data)
        {
            var header = new Byte[12];
            Array.Copy(data, 0, header, 0, 0x0C);
            var cmd = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0x04));
            switch (cmd)
            {
                case 0x08:
                    return "Has connected into room...";
                case 0x05:
                    var text = Encoding.UTF8.GetString(data, 0x0C, data.Length - 0x0C);
                    if (ParseMessage(text) == MessageType.Gift)
                    {
                        return text;
                    }
                    else
                    {
                        return string.Empty;
                    }
                    
                case 0x03:
                    var online = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0x0C));
                    return $"online: {online}";
                default:
                    return string.Empty;
            }

        }

        private MessageType ParseMessage(string message)
        {
            var json = JObject.Parse(message);
            if (json["cmd"] != null)
            {
                if (json.Value<string>("cmd") == "SEND_GIFT")
                {
                    return MessageType.Gift;
                }
                else
                {
                    return MessageType.Dammy;
                }
            }
            else
            {
                return MessageType.Dammy;
            }
        }
    }
}
