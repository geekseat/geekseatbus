using System;
using GeekseatBus.Playground.ServiceA.Messages.Events;

namespace GeekseatBus.Playground.ServiceB.Handlers
{
    public class EventAHandler : IGsHandler<EventA>
    {
        public void Handle(EventA message)
        {
            Console.WriteLine($"Event {message.GetType().Name} ({message.ToJsonString()}) handled.");
        }
    }
}