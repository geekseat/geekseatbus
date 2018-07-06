using System;
using System.Text;
using Newtonsoft.Json;

namespace GeekseatBus.Serializations
{
    public class GsJsonSerializer : IGsSerializer
    {            
        public byte[] Serialize(object source)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
        }

        public byte[] Serialize<T>(T source)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
        }

        public object Deserialize(byte[] source, Type sourceType)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(source), sourceType);
        }

        public T Deserialize<T>(byte[] source)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(source));
        }
    }
}