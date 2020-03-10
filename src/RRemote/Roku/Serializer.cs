using System.IO;
using System.Runtime.Serialization.Json;

namespace RRemote.Roku
{
    public class Serializer
    {
        public static string Serialize<T>(T obj)
        {
            var json = "";
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(ms, obj);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    json = sr.ReadToEnd();
                }
            }

            return json;
        }

        public static T Deserialize<T>(string json)
        {
            T obj;
            using (var ms = new MemoryStream())
            {
                using (var sr = new StreamWriter(ms))
                {
                    sr.Write(json);
                    sr.Flush();
                    ms.Position = 0;
                    var ser = new DataContractJsonSerializer(typeof(T));
                    obj = (T) ser.ReadObject(ms);
                }
            }

            return obj;
        }
    }
}