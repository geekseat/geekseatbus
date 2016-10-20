using System;

namespace GeekseatBus.Serializations
{
    public interface IGsSerializer
    {
        byte[] Serialize(object source);

        byte[] Serialize<T>(T source);

        object Deserialize(byte[] source, Type sourceType);

        T Deserialize<T>(byte[] source);
    }
}