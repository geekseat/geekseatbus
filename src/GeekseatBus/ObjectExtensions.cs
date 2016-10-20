using System;
using System.Text;
using Newtonsoft.Json;

namespace GeekseatBus
{
    public static class ObjectExtensions
    {
        public static string ToJsonString(this object obj)
        {
            return obj == null ? string.Empty : JsonConvert.SerializeObject(obj, Formatting.None);
        }

        public static byte[] Serialize(this object obj)
        {
            return obj == null ? null : Encoding.UTF8.GetBytes(obj.ToJsonString());
        }

        public static string DeserializeText(this byte[] bytes)
        {
            return bytes == null ? string.Empty : Encoding.UTF8.GetString(bytes);
        }

        public static object Deserialize(this byte[] bytes, Type type)
        {
            return bytes == null ? null : JsonConvert.DeserializeObject(bytes.DeserializeText(), type);
        }

        public static T Deserialize<T>(this byte[] bytes)
        {
            return bytes == null ? default(T) : JsonConvert.DeserializeObject<T>(bytes.DeserializeText());
        }
    }
}