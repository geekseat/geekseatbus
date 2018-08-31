using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeekseatBus
{
    public interface IGsHandler<in T>
    {
        Task Handle(IDictionary<string, object> headers, T message);
    }
}