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

        private Option option;

        public DanMuPa():this(new Option { ShowGift = true, ShowDanmu = false, ShowSystem = false })
        {

        }
        public DanMuPa(Option option)
        {
            this.option = option;
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
                    var type = ParseMessage(text);
                    if (type == MessageType.Gift  || type == MessageType.Danmu)
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
                switch (json.Value<string>("cmd"))
                {
                    case "SEND_GIFT":
                        return MessageType.Gift;
                    case "DANMU_MSG":
                        return MessageType.Danmu;
                    default:
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
