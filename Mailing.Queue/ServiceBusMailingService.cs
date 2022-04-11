using Azure.Messaging.ServiceBus;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Mailing.Queue;

public class ServiceBusSettings
{
    public string ConnectionString { get; set; }
    public string MailingQueueName { get; set; }
}

public class ServiceBusMailingService : IMailingService
{
    public ServiceBusSettings _QueueSettings { get; set; }

    public ServiceBusMailingService(IOptions<ServiceBusSettings> queueOptions)
    {
        _QueueSettings = queueOptions.Value;
    }

    public async Task SendResetPasswordEmailAsync(string email, ResetToken resetToken)
    {
        var client = new ServiceBusClient(_QueueSettings.ConnectionString);
        var sender = client.CreateSender(_QueueSettings.MailingQueueName);

        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

        // TODO: create email from template
        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"{{ 'to': '', 'subject': 'Reset Password', 'message': '{resetToken.Value}'}}")))
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
