using System;
using GeekseatBus.Playground.ServiceA.Messages.Commands;
using GeekseatBus.Playground.ServiceA.Messages.Events;

namespace GeekseatBus.Playground.ServiceA.Handlers
{
    public class CommandAHandler : IGsHandler<CommandA>
    {
        private readonly IGsBus _bus;

        public CommandAHandler(IGsBus bus)
        {
            _bus = bus;
        }

        public void Handle(CommandA message)
        {
            Console.WriteLine($"Handling command {message.GetType().Name}: {message.ToJsonString()}");

            var eventMessage = new EventA { StrProps = $"{message.StrProps}, World!" };
            Console.WriteLine($"Publishing event {eventMessage.GetType().Name}: {eventMessage.ToJsonString()}");
            _bus.Publish(eventMessage);
            Console.WriteLine($"Event published!{Environment.NewLine}");
        }
    }
}