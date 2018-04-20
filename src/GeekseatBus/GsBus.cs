using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GeekseatBus.Serializations;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace GeekseatBus
{
    public class GsBus : IGsBus
    {
        private const string MessageNamespaceMarker = ".Messages";
        private const string CommandNamespaceMarker = ".Messages.Commands";
        private const string EventNamespaceMarker = ".Messages.Events";

        private readonly IDictionary<string, Type> _typeMap = new ConcurrentDictionary<string, Type>();
        private readonly GsBusConfig _busConfig;
        private readonly IGsSerializer _serializer;
        private IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IModel _channel;
        private string _serviceQueue;
        private string _consumerTag;
        private bool _disposed;

        public bool IsConnected => (_connection?.IsOpen ?? false) && (_channel?.IsOpen ?? false);

        public GsBus(GsBusConfig busConfig, IServiceCollection serviceCollection = null)
        {
            ConfigureServices(serviceCollection ?? new ServiceCollection());

            _busConfig = busConfig;
            _serializer = _serviceProvider.GetService<IGsSerializer>();
        }

        public GsBus() : this(new GsBusConfig()) { }

        public void Connect()
        {
            if (IsConnected) return;

            var factory = new ConnectionFactory
            {
                HostName = _busConfig.HostName,
                VirtualHost = _busConfig.VirtualHost,
                UserName = _busConfig.UserName,
                Password = _busConfig.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var entryAssembly = Assembly.GetEntryAssembly();
            var entryNamespace = entryAssembly.EntryPoint.DeclaringType.Namespace ?? string.Empty;
            _serviceQueue = entryNamespace;
            _channel.QueueDeclare(_serviceQueue, true, false, false, null);

            if (_busConfig.SendOnly) return;

            Expression<Func<Type, bool>> realTypeFilter =
                t => !t.GetTypeInfo().IsInterface && !t.GetTypeInfo().IsAbstract;
            Expression<Func<Type, bool>> conventionFilter =
                t => !string.IsNullOrEmpty(t.Namespace) &&
                     (t.Namespace.EndsWith(MessageNamespaceMarker) || t.Namespace.EndsWith(CommandNamespaceMarker) ||
                      t.Namespace.EndsWith(EventNamespaceMarker));
            var messageTypeFilter = realTypeFilter.AndAlso(conventionFilter);

            var messageTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(messageTypeFilter.Compile())
                .ToList();

            foreach (var messageType in messageTypes)
            {
                if (!_typeMap.ContainsKey(messageType.FullName))
                    _typeMap.Add(messageType.FullName, messageType);

                // Skip if namespace not ended with .Messages.Events
                if (string.IsNullOrEmpty(messageType.Namespace) ||
                    !messageType.Namespace.EndsWith(EventNamespaceMarker))
                    continue;

                _channel.ExchangeDeclare(messageType.FullName, ExchangeType.Topic, true, false, null);

                if (!messageType.Namespace.StartsWith(entryNamespace))
                {
                    _channel.QueueBind(_serviceQueue, messageType.FullName, "", null);
                }
            }

            DiscoverHandlers();
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            if (_channel != null && _channel.IsOpen)
            {
                if (!string.IsNullOrEmpty(_consumerTag))
                {
                    _channel.BasicCancel(_consumerTag);
                    _consumerTag = string.Empty;
                }

                _channel.Close();
            }

            if (_connection != null && _connection.IsOpen) _connection.Close();
        }

        public void Send<T>(T command)
        {
            if (command == null) return;

            var commandNamespace = command.GetType().Namespace ?? string.Empty;
            if (string.IsNullOrEmpty(commandNamespace) || !commandNamespace.EndsWith(CommandNamespaceMarker))
                return;

            var targetQueueName = commandNamespace.Replace(CommandNamespaceMarker, "");
            var props = new BasicProperties
            {
                Type = command.GetType().FullName
            };

            _channel.BasicPublish("", targetQueueName, props, _serializer.Serialize(command));
        }

        public void Publish<T>(T eventMessage)
        {
            if (eventMessage == null || string.IsNullOrEmpty(eventMessage.GetType().Namespace) ||
                !eventMessage.GetType().Namespace.EndsWith(EventNamespaceMarker))
                return;

            var targetExchange = eventMessage.GetType().FullName;
            var props = new BasicProperties
            {
                Type = eventMessage.GetType().FullName
            };

            _channel.BasicPublish(targetExchange, "", props, _serializer.Serialize(eventMessage));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            Disconnect();

            if (disposing)
            {
                _channel?.Dispose();
                _connection?.Dispose();
            }

            _typeMap?.Clear();
            _disposed = true;
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            var handlerBaseType = typeof(IGsHandler<>);
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces()
                             .Any(x => x.GetTypeInfo().IsGenericType &&
                                       x.GetGenericTypeDefinition() == handlerBaseType))
                .ToList();
            foreach (var handlerType in handlerTypes)
            {
                var baseCloseGenericTypes = handlerType.GetInterfaces()
                    .Where(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == handlerBaseType)
                    .ToList();

                foreach (var baseCloseGenericType in baseCloseGenericTypes)
                {
                    serviceCollection.AddTransient(baseCloseGenericType, handlerType);
                }
            }

            if (serviceCollection.All(sd => sd.ServiceType != typeof(IGsSerializer)))
            {
                serviceCollection.AddTransient<IGsSerializer>(sp => new GsAvroSerializer());
            }

            serviceCollection.AddSingleton<IGsBus>(this);

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private void DiscoverHandlers()
        {
            var messageHandlers = new MessageHandlerBuilder(_serviceProvider);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (sender, message) =>
            {
                var props = message.BasicProperties;
                var actualMessageType = _typeMap[props.Type];
                var actualMessage = _serializer.Deserialize(message.Body, actualMessageType);

                typeof(MessageHandlerBuilder).GetMethod("HandleMessages")
                    .MakeGenericMethod(actualMessage.GetType()).Invoke(messageHandlers, new[] { actualMessage });
            };

            _consumerTag = _channel.BasicConsume(consumer, _serviceQueue, true);
        }
    }
}