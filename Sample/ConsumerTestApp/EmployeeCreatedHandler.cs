using System;
using System.Collections.Generic;
using GeekseatBus;
using Handler.Messages.Commands;
using Handler.Messages.Events;
using Newtonsoft.Json;

namespace Handler
{
    public class EmployeeCreatedHandler : IGsHandler<CreateEmployee>
    {
        private readonly IGsBus _bus;

        public EmployeeCreatedHandler(IGsBus bus)
        {
            _bus = bus;
        }

        public void Handle(IDictionary<string, object> headers, CreateEmployee message)
        {
            var msgName = message.GetType().Name;

            string tenantId = GetTenantId(headers);

            Console.WriteLine($"Handle {msgName}:\nTenantId: {tenantId}\nBody: {JsonConvert.SerializeObject(message, Formatting.Indented)}");

            Console.WriteLine("Press any key to handle the message or press e to simulate exception.");

            if (Console.ReadKey().Key == ConsoleKey.E)
            {
                var msg = $"Cannot handle message: {msgName}";

                Console.WriteLine(msg);

                throw new Exception(msg);
            }

            Console.WriteLine("Message is handled");

            _bus.Publish(headers, new EmployeeCreatedEvent
            {
                Id = message.Id,
                Name = message.Name,
                Address = message.Address
            });
        }

        private static string GetTenantId(IDictionary<string, object> headers)
        {
            var encodedTenantId = headers != null && headers.ContainsKey("tenantId") ? headers["tenantId"] as byte[] : null;

            return encodedTenantId == null ? null : System.Text.Encoding.UTF8.GetString(encodedTenantId);
        }
    }
}
