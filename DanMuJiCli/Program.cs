using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DanMuJiCli
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = DanMuJi.DanMuJiFactory.Make(DanMuJi.DanMuJiFactory.ParseUrl(args[0]));
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
