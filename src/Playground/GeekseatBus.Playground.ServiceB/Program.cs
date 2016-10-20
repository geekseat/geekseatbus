using System;

namespace GeekseatBus.Playground.ServiceB
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var bus = new GsBus())
            {
                bus.Connect();
                Console.WriteLine("Service B is running....");
                Console.WriteLine("Press ENTER to terminate...");
                Console.ReadLine();
            }
        }
    }
}
