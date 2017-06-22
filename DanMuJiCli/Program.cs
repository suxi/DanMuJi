using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanMuJiCli
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new DanMuJi.Bilibili.DanMuji();
            if (args.Length < 1)
            {
                Console.WriteLine("DanMuJiCli <url>");
            }
            else
            {
                client.ConnectAsync(args[0]).Wait();
            }
            

        }
    }
}
