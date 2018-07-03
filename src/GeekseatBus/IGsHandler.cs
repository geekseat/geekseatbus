using System.Collections.Generic;

namespace GeekseatBus
{
    public interface IGsHandler<in T>
    {
        void Handle(IDictionary<string, object> headers, T message);
    }
}