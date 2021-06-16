using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace azure_serverless_ASB_producer_consumer
{
    public class Producer
    {
        // the client that owns the connection and can be used to create senders and receivers
        private ServiceBusClient _client;

        // the sender used to publish messages to the queue
        private ServiceBusSender _sender;

        private readonly AzureSettings _azureSettings;
        private readonly ILogger<Producer> _log;

        public Producer(IOptions<AzureSettings> options, ILogger<Producer> log)
        {
            _azureSettings = options.Value;
            _log = log;
            _client = new ServiceBusClient(_azureSettings.AzureServiceBusConnectionString);
            _sender = _client.CreateSender(_azureSettings.AzureServiceBusReservationQueueName);
        }

        [FunctionName("Producer")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _log.LogInformation("Creating reservation ...");

            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //

            try
            {
                // send a batch of messages to the queue
                await SendMessages();
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await _sender.DisposeAsync();
                await _client.DisposeAsync();
            }
            
            _log.LogInformation("Press any key to end the application");

            return new OkResult();
        }


        Queue<ServiceBusMessage> CreateMessages()
        {
            // create a queue containing the messages and return it to the caller
            Queue<ServiceBusMessage> messages = new Queue<ServiceBusMessage>();
            messages.Enqueue(new ServiceBusMessage("{reservationid:1}"));
            return messages;
        }

        async Task SendMessages()
        {
            // get the messages to be sent to the Service Bus queue
            Queue<ServiceBusMessage> messages = CreateMessages();

            // total number of messages to be sent to the Service Bus queue
            int messageCount = messages.Count;

            // while all messages are not sent to the Service Bus queue
            while (messages.Count > 0)
            {
                // start a new batch 
                using ServiceBusMessageBatch messageBatch = await _sender.CreateMessageBatchAsync();

                // add the first message to the batch
                if (messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue once the message is added to the batch
                    messages.Dequeue();
                }
                else
                {
                    // if the first message can't fit, then it is too large for the batch
                    throw new Exception($"Message {messageCount - messages.Count} is too large and cannot be sent.");
                }

                // add as many messages as possible to the current batch
                while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue as it has been added to the batch
                    messages.Dequeue();
                }

                // now, send the batch
                await _sender.SendMessagesAsync(messageBatch);

                // if there are any remaining messages in the .NET queue, the while loop repeats 
            }

            _log.LogInformation($"Sent a batch of {messageCount} messages to the topic: {_azureSettings.AzureServiceBusReservationQueueName}");
        }
    }
}
