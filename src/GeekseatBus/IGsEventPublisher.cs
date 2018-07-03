using System.Collections.Generic;

namespace GeekseatBus
{
    public interface IGsEventPublisher
    {
        void Publish<T>(T eventMessage);
        void Publish<T>(IDictionary<string, object> headers, T eventMessage);
    }
}