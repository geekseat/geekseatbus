using System.Collections.Generic;

namespace GeekseatBus
{
    public interface IGsCommandSender
    {
        void Send<T>(T command);
        void Send<T>(IDictionary<string, object> headers, T command);
    }
}