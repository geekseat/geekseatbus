using System;
using Microsoft.Hadoop.Avro;

namespace GeekseatBus.Serializations
{
    public class GsAvroSerializer : IGsSerializer
    {
        private readonly DynamicAvroSerializer _serializer = new DynamicAvroSerializer();

        public byte[] Serialize(object source)
        {
            return _serializer.Serialize(source);
        }

        public byte[] Serialize<T>(T source)
        {
            return _serializer.Ser(source);
        }

        public object Deserialize(byte[] source, Type sourceType)
        {
            return _serializer.Deserialize(source, sourceType);
        }

        public T Deserialize<T>(byte[] source)
        {
            var result = _serializer.Deser<T>(source);
            if (result == null) return default(T);

            return (T)result;
        }
    }
}