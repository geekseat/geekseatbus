using System;
using System.Collections.Generic;
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

        public void HandleMessages<T>(Dictionary<string, object> headers, T message)
        {
            var handlers = _serviceProvider.GetServices<IGsHandler<T>>();

            foreach (var handler in handlers)
            {
                handler.Handle(headers, message);
            }
        }
    }
}