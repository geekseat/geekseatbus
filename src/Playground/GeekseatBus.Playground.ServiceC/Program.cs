using System;

namespace GeekseatBus.Playground.ServiceC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var bus = new GsBus())
            {
                bus.Connect();
                Console.WriteLine("Service C is running....");
                Console.WriteLine("Press ENTER to terminate...");
                Console.ReadLine();
            }
        }
    }
}
