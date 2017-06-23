using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace DanMuJi
{
    public interface IDanMuJi
    {
        Task ConnectAsync(string url);

    }

    public class Platforms : Dictionary<TypeInfo, List<string>>
    {
        public Platforms()
        {
            Add(typeof(Bilibili.DanMuJi).GetTypeInfo(), new List<string>() { "live.bilibili.com" });
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
}
