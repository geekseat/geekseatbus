using System;
using GeekseatBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Handler
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();

            var bus = new GsBus(new GsBusConfig
            {
                HostName = config["RmqHostName"],
                UserName = config["RmqUserName"],
                Password = config["RmqPassword"],
                VirtualHost = config["RmqVHost"],
                SerializerType = SerializerType.Json
                //SendOnly = false
            }, services);

            bus.Connect();

            Console.WriteLine("Consumer is started. Press key to exit!");

            Console.ReadKey();
        }
    }
}
