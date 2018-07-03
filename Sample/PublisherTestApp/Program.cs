using System;
using System.Collections.Generic;
using GeekseatBus;
using Handler.Messages.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Publisher
{
    public class Program
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
                //SendOnly = false
            }, services);

            bus.Connect();

            Console.WriteLine("Publisher is started. Press any key to send message or q to exit!");

            while (Console.ReadKey().Key != ConsoleKey.Q)
            {
                var id = Guid.NewGuid();

                var message = new CreateEmployee
                {
                    Id = id,
                    Name = $"User Name is {id}",
                    Address = $"Address for user {id}"
                };

                var header = new Dictionary<string, object>
                {
                    ["tenantId"] = "d1cfef8f-3a6e-44e8-a5ed-34a34230e2ef"
                };

                bus.Send(header, message);

                Console.WriteLine($"Message is sent:\n {JsonConvert.SerializeObject(message, Formatting.Indented)}");
            }

            Console.ReadKey();
        }
    }
}
