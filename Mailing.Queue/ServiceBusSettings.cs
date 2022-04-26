namespace Mailing.Queue;

public class ServiceBusSettings
{
    public string ConnectionString { get; set; }
    public string MailingQueueName { get; set; }
}
