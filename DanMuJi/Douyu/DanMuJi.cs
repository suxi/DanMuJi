using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanMuJi.Douyu
{
    class DanMuJi : IDanMuJi
    {
        private DanMuPa Parser;
        
        public DanMuJi()
        {
            Parser = new DanMuPa();
        }
        public async Task ConnectAsync(string url, Option? option)
        {
            // 获取roomid
            var uri = new Uri(url);
            var roomId = long.Parse(uri.AbsolutePath.Substring(1));

            // 链接弹幕服务器
            using (var tcpClient = new TcpClient())
            {
                Console.WriteLine($"Connect to Room {roomId} on Douyu...");
                await tcpClient.ConnectAsync("openbarrage.douyutv.com", 8601);
                using (var stream = tcpClient.GetStream())
                {


                    var data = EnterRoom(roomId);
                    Console.WriteLine($"Login into room {roomId}");
                    Debug.WriteLine(BitConverter.ToString(data));
                    await stream.WriteAsync(data, 0, data.Length);
                    var d = new byte[4];
                    await stream.ReadAsync(d, 0, 4);
                    var len = BitConverter.ToInt32(d, 0);
                    d = new byte[len];
                    await stream.ReadAsync(d, 0, len);
                    Debug.WriteLine(BitConverter.ToString(d));
                    Console.WriteLine(Parser.ParsePackge(d));
                    data = EnterGroup(roomId);
                    Console.WriteLine($"Enter info group -9999...");
                    Debug.WriteLine(BitConverter.ToString(data));
                    await stream.WriteAsync(data, 0, data.Length);

                    var runner = new TcpRunner(stream, () =>
                    {
                        return Heartbeat();
                    }, (buffer) =>
                    {
                        var respText = Parser.ParsePackge(buffer);
                        if (!string.IsNullOrWhiteSpace(respText))
                        {
                            Console.WriteLine(respText);
                        }
                    },true,false);

                    runner.run();
                    //var heartbeat = Task.Run(() =>
                    //{
                    //    var at = DateTimeOffset.Now.ToUnixTimeSeconds();
                    //    while (true)
                    //    {
                    //        if (tcpClient.Connected && stream.CanWrite)
                    //        {
                    //            SpinWait.SpinUntil(() => DateTimeOffset.Now.ToUnixTimeSeconds() - at >= 45);
                    //            Byte[] heartBeat = Heartbeat();
                    //            stream.Write(heartBeat, 0, heartBeat.Length);
                    //            at = DateTimeOffset.Now.ToUnixTimeSeconds();
                    //            Debug.WriteLine("Heartbeat");
                    //        }
                    //        else
                    //        {
                    //            Debug.WriteLine("Disconnect");
                    //            return;
                    //        }
                    //    }
                    //});

                    //var reciever = Task.Run(() =>
                    //{
                    //    var buffer = new Byte[4];
                    //    var at = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    //    while (true)
                    //    {
                    //        if (tcpClient.Available > 0 && stream.CanRead)
                    //        {
                    //            buffer = new Byte[4];
                    //            stream.Read(buffer, 0, 4);
                    //            var MsgLength = BitConverter.ToInt32(buffer, 0);
                    //            buffer = new Byte[MsgLength];
                    //            var recievedByte = 0;
                    //            while (recievedByte < MsgLength)
                    //            {
                    //                recievedByte += stream.Read(buffer, recievedByte, MsgLength - recievedByte);
                    //            }
                    //            var respText = Parser.ParsePackge(buffer);
                    //            if (!string.IsNullOrWhiteSpace(respText))
                    //            {
                    //                Console.WriteLine(respText);
                    //            }
                    //            Debug.WriteLine(BitConverter.ToString(buffer));
                    //        }
                    //        else
                    //        {

                    //            SpinWait.SpinUntil(() => DateTimeOffset.Now.ToUnixTimeMilliseconds() - at >= 300);
                    //            at = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    //        }
                    //    }
                    //});
                    //reciever.Wait();

                }
            }

        }

        private byte[] Encode(byte[] data)
        {
            var message = new List<byte>();
            message.AddRange(BitConverter.GetBytes((Int16)689));
            message.Add(0x00);
            message.Add(0x00);
            message.AddRange(data);
            var length = BitConverter.GetBytes(message.Count + 4);
            message.InsertRange(0, length);
            message.InsertRange(0, length);
            return message.ToArray();
        }

        private byte[] Heartbeat()
        {
            var msg = $"type@=keeplive/tick@={DateTimeOffset.Now.ToUnixTimeSeconds()}/\0";
            var data = Encode(Encoding.UTF8.GetBytes(msg));
            return data;
        }

        private byte[] EnterRoom(long roomId)
        {
            var msg = $"type@=loginreq/roomid@={roomId}/\0";
            var data = Encode(Encoding.UTF8.GetBytes(msg));
            return data;
        }
        private byte[] EnterGroup(long roomId)
        {
            var msg = $"type@=joingroup/rid@={roomId}/gid@=0/\0";
            var data = Encode(Encoding.UTF8.GetBytes(msg));
            return data;
        }
    }
}
