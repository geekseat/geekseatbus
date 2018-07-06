namespace GeekseatBus
{
    public class GsBusConfig
    {
        public string HostName { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool SendOnly { get; set; }
        public SerializerType SerializerType { get; set; }

        public GsBusConfig()
        {
            HostName = "localhost";
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            SerializerType = SerializerType.Avro;
        }
    }
}