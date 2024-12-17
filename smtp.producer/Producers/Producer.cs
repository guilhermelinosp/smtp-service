using System.Text;
using RabbitMQ.Client;

namespace smtp.producer.Producers;

public class Producer(IConfiguration configuration)
{
	private static IChannel? _channel;
	private static string? _queueName;

	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		_queueName = configuration["RabbitMQ:QueueName"]!;

		var connectionFactory = new ConnectionFactory
		{
			HostName = configuration["RabbitMQ:HostName"]!,
			Port = Convert.ToInt32(configuration["RabbitMQ:Port"]!),
			UserName = configuration["RabbitMQ:UserName"]!,
			Password = configuration["RabbitMQ:Password"]!,
		};

		var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

		_channel = await connection.CreateChannelAsync(null, cancellationToken);

		await _channel.QueueDeclareAsync(
			queue: _queueName,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null,
			cancellationToken: cancellationToken);
	}

	public static async Task SendAsync(string message, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var body = Encoding.UTF8.GetBytes(message);
		await _channel!.BasicPublishAsync(
			exchange: string.Empty,
			routingKey: _queueName!,
			body: body,
			cancellationToken: cancellationToken);
	}
}