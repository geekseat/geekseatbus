using System;
using System.Collections.Generic;
using System.Text;

namespace Handler.Messages.Events
{
    public class EmployeeCreatedEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }
}
