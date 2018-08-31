using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GeekseatBus
{
    internal class MessageHandlerBuilder
    {
        private readonly IServiceProvider _serviceProvider;

        public MessageHandlerBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleMessagesAsync<T>(Dictionary<string, object> headers, T message)
        {
            var handlers = _serviceProvider.GetServices<IGsHandler<T>>();

            foreach (var handler in handlers)
            {
                await handler.Handle(headers, message);
            }
        }
    }
}