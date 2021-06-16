using System;
using System.Collections.Generic;
using System.Text;

namespace azure_serverless_ASB_producer_consumer
{
    public class AzureSettings
    {
        public string AzureServiceBusConnectionString { get; set; }
        public string AzureServiceBusReservationQueueName { get; set; }
    }
}
