using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DanMuJi.Bilibili
{
    public class DanMuji
    {
        private TcpClient tcpClient;
        private Task heartbeat;
        private Task reciever;

        public async Task ConnectAsync(string url)
        {
            using (tcpClient = new TcpClient())
            {

                // 获得roomid
                var httpClient = new HttpClient();
                var bodyText = await httpClient.GetStringAsync(url);
                var reg = new Regex(@"var\s+ROOMID\s+=\s+(\w+);");
                var match = reg.Match(bodyText);
                var roomId = match.Groups[1].Value;
                // 获得tcp服务器地址

                var xmlText = await httpClient.GetStringAsync($"http://live.bilibili.com/api/player?id=cid:{roomId}");
                var xml = XElement.Parse($"<root>{xmlText}</root>");
                var host = xml.Element("dm_server").Value;
                var port = int.Parse(xml.Element("dm_port").Value);

                // 链接房间
                var uid = (long)(100000000000000.0 + 200000000000000.0 * new Random().NextDouble());
                var param2 = $"{{\"roomid\":{roomId},\"uid\":{uid}}}";
                byte[] head = { 0x00, 0x10, 0x00, 0x01 };
                byte[] cmd = { 0x00, 0x00, 0x00, 0x07 };
                byte[] param1 = { 0x00, 0x00, 0x00, 0x01 };
                var data = new List<byte>();
                data.AddRange(head);
                data.AddRange(cmd);
                data.AddRange(param1);
                data.AddRange(Encoding.ASCII.GetBytes(param2));
                Int32 length = data.Count + 4;
                var header = BitConverter.GetBytes(length);
                Array.Reverse(header);
                data.InsertRange(0, header);
                Console.WriteLine($"Connect to Room {roomId} on {host}:{port}");
                tcpClient.NoDelay = true;
                await tcpClient.ConnectAsync(host, port);
                var parser = new DanMuPa();
                using (var stream = tcpClient.GetStream())
                {
                    stream.Write(data.ToArray(), 0, data.Count);

                    heartbeat = Task.Run(() =>
                    {
                        var at = DateTimeOffset.Now.ToUnixTimeSeconds();
                        Byte[] heartBeat = { 0x00, 0x00, 0x00, 0x10,
                                             0x00, 0x10, 0x00, 0x01,
                                             0x00, 0x00, 0x00, 0x02,
                                             0x00, 0x00, 0x00, 0x01 };
                        while (true)
                        {
                            if (tcpClient.Connected && stream.CanWrite)
                            {
                                SpinWait.SpinUntil(() => DateTimeOffset.Now.ToUnixTimeSeconds() - at >= 29);
                                stream.Write(heartBeat, 0, heartBeat.Length);
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

                    reciever = Task.Run(() =>
                    {
                        var buffer = new Byte[4];
                        var at = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        while (true)
                        {
                            if (tcpClient.Available > 0 && stream.CanRead)
                            {
                                buffer = new Byte[4];
                                stream.Read(buffer, 0, 4);
                                var MsgLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0)) - 4;
                                buffer = new Byte[MsgLength];
                                var recievedByte = 0;
                                while (recievedByte < MsgLength)
                                {
                                    recievedByte += stream.Read(buffer, recievedByte, MsgLength - recievedByte);
                                }
                                var respText = parser.ParsePackage(buffer);
                                if (!string.IsNullOrWhiteSpace(respText))
                                {
                                    Console.WriteLine(respText);
                                }
                                Debug.WriteLine(BitConverter.ToString(buffer));
                            }
                            else
                            {

                                SpinWait.SpinUntil(() => DateTimeOffset.Now.ToUnixTimeMilliseconds() - at >= 300);
                                at = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            }
                        }
                    });

                    reciever.Wait();
                }

            }
        }


    }
}
