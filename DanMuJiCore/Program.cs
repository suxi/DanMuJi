using System;

namespace DanMuJiCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new DanMuJi.Bilibili.DanMuJi();
            if (args.Length < 1)
            {
                Console.WriteLine("DanMuJiCore <url>");
            }
            else
            {
                client.ConnectAsync(args[0]).Wait();
            }
            
        }
    }
}