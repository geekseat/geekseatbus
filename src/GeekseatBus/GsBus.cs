﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using GeekseatBus.Serializations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
        private const string DeadLetterQueue = "dead.letter.queue";
        private const int RetryDelay = 1000; //ms
        private const int MaxRetryCount = 5;

        private readonly ConcurrentDictionary<string, Type> _typeMap = new ConcurrentDictionary<string, Type>();
        private readonly GsBusConfig _busConfig;
        private readonly IServiceCollection _serviceCollection;
        private IGsSerializer _serializer;
        private IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IModel _channel;
        private string _serviceQueue;
        private string _consumerTag;
        private bool _disposed;
        private readonly ISet<string> _eventHandlerTypes = new HashSet<string>();

        public bool IsConnected => (_connection?.IsOpen ?? false) && (_channel?.IsOpen ?? false);

        public GsBus(GsBusConfig busConfig, IServiceCollection serviceCollection = null)
        {
            ConfigureServices(serviceCollection ?? new ServiceCollection());

            _busConfig = busConfig;
            _serviceCollection = serviceCollection;            
            //_serializer = _serviceProvider.GetService<IGsSerializer>();
        }

        public GsBus() : this(new GsBusConfig())
        {

        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private IServiceProvider GetServiceProvider()
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = _serviceCollection.BuildServiceProvider();
            }

            return _serviceProvider;
        }

        private IGsSerializer GetSerializer()
        {
            if (_serializer == null)
            {
                _serializer = GetServiceProvider().GetService<IGsSerializer>();
            }

            return _serializer;
        }

        public void Connect()
        {
            if (IsConnected) return;

            var factory = new ConnectionFactory
            {
                HostName = _busConfig.HostName,
                VirtualHost = _busConfig.VirtualHost,
                UserName = _busConfig.UserName,
                Password = _busConfig.Password,
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = 5,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var entryAssembly = Assembly.GetEntryAssembly();
            var entryNamespace = entryAssembly.EntryPoint.DeclaringType.Namespace ?? string.Empty;
            _serviceQueue = entryNamespace;
            _channel.QueueDeclare(DeadLetterQueue, true, false, false);
            _channel.QueueDeclare(_serviceQueue, true, false, false);
            _channel.BasicQos(0, 1, false);

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
                    _typeMap.TryAdd(messageType.FullName, messageType);

                // Skip if namespace not ended with .Messages.Events
                if (string.IsNullOrEmpty(messageType.Namespace) ||
                    !messageType.Namespace.EndsWith(EventNamespaceMarker))
                    continue;

                _channel.ExchangeDeclare(messageType.FullName, ExchangeType.Topic, true, false, null);

                if (_eventHandlerTypes.Contains(messageType.FullName))
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
            Send(null, command);
        }

        public void Send<T>(IDictionary<string, object> headers, T command)
        {
            if (command == null) return;

            var commandNamespace = command.GetType().Namespace ?? string.Empty;
            if (string.IsNullOrEmpty(commandNamespace) || !commandNamespace.EndsWith(CommandNamespaceMarker))
                return;

            var targetQueueName = commandNamespace.Replace(CommandNamespaceMarker, "");
            var props = new BasicProperties
            {
                Type = command.GetType().FullName,
                Headers = headers
            };

            lock (_channel)
            {
                _channel.BasicPublish("", targetQueueName, props, GetSerializer().Serialize(command));
            }
        }

        public void Publish<T>(T eventMessage)
        {
            Publish(null, eventMessage);
        }

        public void Publish<T>(IDictionary<string, object> headers, T eventMessage)
        {
            if (eventMessage == null || string.IsNullOrEmpty(eventMessage.GetType().Namespace) ||
                !eventMessage.GetType().Namespace.EndsWith(EventNamespaceMarker))
                return;

            var targetExchange = eventMessage.GetType().FullName;
            var props = new BasicProperties
            {
                Type = eventMessage.GetType().FullName,
                Headers = headers
            };

            lock (_channel)
            {
                _channel.BasicPublish(targetExchange, "", props, GetSerializer().Serialize(eventMessage));
            }
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
                    var messageType = baseCloseGenericType.GetGenericArguments()[0];

                    if (messageType.Namespace.EndsWith(EventNamespaceMarker))
                    {
                        // this is event handlers
                        _eventHandlerTypes.Add(messageType.FullName);
                    }

                    serviceCollection.AddTransient(baseCloseGenericType, handlerType);
                }
            }

            if (serviceCollection.All(sd => sd.ServiceType != typeof(IGsSerializer)))
            {
                serviceCollection.AddTransient<IGsSerializer>(sp => CreateSerializer());
            }

            serviceCollection.AddSingleton<IGsBus>(this);

            //_serviceProvider = _serviceProvider ?? serviceCollection.BuildServiceProvider();

            //_serializer = _serviceProvider.GetService<IGsSerializer>();
        }

        private IGsSerializer CreateSerializer()
        {
            if (_busConfig?.SerializerType == SerializerType.Avro)
            {
                return new GsAvroSerializer();
            }

            return new GsJsonSerializer();
        }

        private void DiscoverHandlers()
        {
            var serviceProvider = GetServiceProvider();
            var messageHandlers = new MessageHandlerBuilder(serviceProvider);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, message) =>
            {
                var props = message.BasicProperties;                
                var deliveryTag = message.DeliveryTag;
                var headers = props.Headers;

                if (!_typeMap.ContainsKey(props.Type) || !_typeMap.TryGetValue(props.Type, out Type actualMessageType))
                {
                    return;
                }

                var actualMessage = GetSerializer().Deserialize(message.Body, actualMessageType);

                var method = typeof(MessageHandlerBuilder).GetMethod("HandleMessagesAsync").MakeGenericMethod(actualMessage.GetType());

                var (canHandle, errorMessage) = await HandleMessage(method, messageHandlers, headers, actualMessage, deliveryTag);

                if (!canHandle)
                {
                    SendToDeadLetterQueue(message.BasicProperties, message.Body);
                }
            };

            lock (_channel)
            {
                _consumerTag = _channel.BasicConsume(consumer, _serviceQueue, false);
            }
        }

        private async Task<(bool, string)> HandleMessage(MethodInfo method, MessageHandlerBuilder messageHandlers, IDictionary<string, object> headers, object actualMessage, ulong deliveryTag)
        {
            var retryCount = 0;
            Retry:
            try
            {
                await (Task)method.Invoke(messageHandlers, new[] { headers, actualMessage });

                lock (_channel)
                {
                    _channel.BasicAck(deliveryTag, false);
                }

                //Console.WriteLine("Ack");

                return (true, null);
            }
            catch (Exception ex)
            {
                var body = JsonConvert.SerializeObject(actualMessage);

                var message = $"Error while handle message: {actualMessage.GetType().FullName}\n{ex.Message}\n{ex.StackTrace}";

                Console.WriteLine(message);

                if (retryCount < MaxRetryCount)
                {
                    await Task.Delay(RetryDelay);

                    retryCount++;

                    //todo log retrying                    
                    goto Retry;
                }

                lock (_channel)
                {
                    //send to dead.letter exchange
                    _channel.BasicNack(deliveryTag, false, false);
                }

                //todo log error
                return (false, message);
            }
        }

        private void SendToDeadLetterQueue(IBasicProperties properties, byte[] bodyBytes)
        {
            lock (_channel)
            {
                _channel.BasicPublish("", DeadLetterQueue, properties, bodyBytes);
            }
        }
    }
}