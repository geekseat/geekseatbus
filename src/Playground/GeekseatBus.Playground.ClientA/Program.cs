using System;
using GeekseatBus.Playground.ServiceA.Messages.Commands;

namespace GeekseatBus.Playground.ClientA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var bus = new GsBus())
            {
                Console.WriteLine("Client started!");

                while (true)
                {
                    Console.Write("Press ENTER to send command or 'q' to quit: ");
                    var key = Console.ReadLine();
                    if ("q".Equals(key, StringComparison.OrdinalIgnoreCase)) return;

                    if (!bus.IsConnected) bus.Connect();

                    var cmd = new CommandA { StrProps = "Hello" };
                    Console.WriteLine($"Sending command: {cmd.ToJsonString()}");
                    bus.Send(cmd);
                }
            }
        }
    }
}
