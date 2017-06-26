using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace DanMuJi
{
    public interface IDanMuJi
    {
        Task ConnectAsync(string url, Option? option = null);

    }

    public struct Option
    {
        public bool ShowDanmu;
        public bool ShowGift;
        public bool ShowSystem;
    }

    public class Platforms : Dictionary<TypeInfo, List<string>>
    {
        public Platforms()
        {
            Add(typeof(Bilibili.DanMuJi).GetTypeInfo(), new List<string>() { "live.bilibili.com" });
            Add(typeof(Douyu.DanMuJi).GetTypeInfo(), new List<string>() { "https://www.douyu.com" });
        }
    }


    public class DanMuJiFactory
    {
        public static IDanMuJi Make(TypeInfo t)
        {
            var constructor = t.GetConstructor(Type.EmptyTypes);
            var danmuji = (IDanMuJi)constructor.Invoke(null);
            return danmuji;
        }

        public static TypeInfo ParseUrl(string url)
        {
            var platforms = new Platforms();
            foreach (var platform in platforms)
            {
                foreach (var u in platform.Value)
                {
                    if (url.Contains(u))
                    {
                        return platform.Key;
                    }
                }
            }

            throw new KeyNotFoundException();
        }
    }

    public delegate byte[] OnHeartbeat();
    public delegate void OnMessage(byte[] data);
    public class TcpRunner
    {
        private Stream stream;
        private OnHeartbeat onHeartbeat;
        private OnMessage onMessage;
        private bool isLittle;

        public TcpRunner(Stream stream, OnHeartbeat onHeartbeat, OnMessage onMessage, bool isLittle = true)
        {
            this.stream = stream;
            this.onHeartbeat = onHeartbeat;
            this.onMessage = onMessage;
            this.isLittle = isLittle;
        }

        public void run()
        {
            var beat = Task.Run(async () =>
            {
                var at = DateTimeOffset.Now.ToUnixTimeSeconds();
                while (true)
                {
                    if (stream.CanWrite)
                    {
                        SpinWait.SpinUntil(() => DateTimeOffset.Now.ToUnixTimeSeconds() - at >= 29);
                        var data = onHeartbeat();
                        await stream.WriteAsync(data, 0, data.Length);
                        at = DateTimeOffset.Now.ToUnixTimeSeconds();
                        Debug.WriteLine("Heartbeat");
                    }
                    else
                    {
                        Debug.WriteLine("Disconnect");
                        return;
                    }
                }
            });

            var runner = Task.Run(async () =>
            {
                var buffer = new Byte[4];
                while (true)
                {
                    if (stream.CanRead)
                    {
                        buffer = new Byte[4];
                        await stream.ReadAsync(buffer, 0, 4);
                        var len = BitConverter.ToInt32(buffer, 0);
                        if (isLittle == false)
                        {
                            len = IPAddress.NetworkToHostOrder(len);
                        }
                        var MsgLength = len;
                        buffer = new Byte[MsgLength];
                        var recievedByte = 0;
                        while (recievedByte < MsgLength)
                        {
                            recievedByte += await stream.ReadAsync(buffer, recievedByte, MsgLength - recievedByte);
                        }
                        onMessage(buffer);
                        Debug.WriteLine(BitConverter.ToString(buffer));
                    }
                }
            });

            runner.Wait();
        }

        ~TcpRunner()
        {
            stream = null;
        }
    }
}
