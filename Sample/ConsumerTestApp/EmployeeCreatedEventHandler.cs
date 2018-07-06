using System;
using System.Collections.Generic;
using GeekseatBus;
using Handler.Messages.Commands;
using Handler.Messages.Events;
using Newtonsoft.Json;

namespace Handler
{
    public class EmployeeCreatedEventHandler : IGsHandler<EmployeeCreatedEvent>
    {
        public void Handle(IDictionary<string, object> headers, EmployeeCreatedEvent message)
        {
            var msgName = message.GetType().Name;

            string tenantId = GetTenantId(headers);

            Console.WriteLine($"Handle {msgName}:\nTenantId: {tenantId}\nBody: {JsonConvert.SerializeObject(message, Formatting.Indented)}");
        }

        private static string GetTenantId(IDictionary<string, object> headers)
        {
            var encodedTenantId = headers != null && headers.ContainsKey("tenantId") ? headers["tenantId"] as byte[] : null;

            return encodedTenantId == null ? null : System.Text.Encoding.UTF8.GetString(encodedTenantId);
        }
    }
}
