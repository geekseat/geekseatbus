using System;
using GeekseatBus;
using Handler.Messages.Commands;
using Newtonsoft.Json;

namespace Handler
{
    public class EmployeeCreatedHandler : IGsHandler<CreateEmployee>
    {
        public void Handle(CreateEmployee message)
        {
            var msgName = message.GetType().Name;

            Console.WriteLine($"Handle {msgName}:\n{JsonConvert.SerializeObject(message, Formatting.Indented)}");

            Console.WriteLine("Press any key to handle the message or press e to simulate exception.");

            if (Console.ReadKey().Key == ConsoleKey.E)
            {
                var msg = $"Cannot handle message: {msgName}";

                Console.WriteLine(msg);

                throw new Exception(msg);
            }

            Console.WriteLine("Message is handled");
        }
    }
}
