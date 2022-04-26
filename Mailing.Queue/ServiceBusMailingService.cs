using Azure.Messaging.ServiceBus;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Mailing.Queue;

public class ServiceBusMailingService : IMailSender
{
    public ServiceBusSettings _QueueSettings { get; set; }

    public ServiceBusMailingService(IOptions<ServiceBusSettings> queueOptions)
    {
        _QueueSettings = queueOptions.Value;
    }

    public async Task SendEmailAsync(Email email)
    {
        var client = new ServiceBusClient(_QueueSettings.ConnectionString);
        var sender = client.CreateSender(_QueueSettings.MailingQueueName);

        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

        var body = JsonSerializer.Serialize(email);
        if (!messageBatch.TryAddMessage(new ServiceBusMessage(body)))
        {
            throw new Exception($"The message is too large to fit in the batch.");
        }

        try
        {
            await sender.SendMessagesAsync(messageBatch);
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
    }
}
