using System;

namespace Handler.Messages.Commands
{
    public class CreateEmployee
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address  { get; set; }
    }
}
