using System.Text;
using System.Text.Json;
using ChatApp.Core.Models;
using ChatApp.Messaging.Configuration;
using ChatApp.Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ChatApp.Messaging.Services
{
    public class RabbitMqService : IMessageQueueService, IAsyncDisposable
    {
        private IConnection _connection = null!;
        private IChannel _channel = null!;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly RabbitMqOptions _options = null!;
        private string _exchangeName = "";

        public RabbitMqService(ILogger<RabbitMqService> logger, IOptions<RabbitMqOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task InitializeAsync()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };
            _exchangeName = _options.ExchangeName;

            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Connection to RabbitMq could not be established!\n{Ex}", ex);
                throw;
            }

            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Fanout,
                durable: false,
                autoDelete: false);
        }

        public async Task PublishAsync(ChatMessageModel message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: "",
                mandatory: true,
                body: body);
        }

        public async Task StartConsumingAsync(Func<ChatMessageModel, Task> onMessageReceived)
        {
            var declareOk = await _channel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null);

            string queueName = declareOk.QueueName;

            await _channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: "");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<ChatMessageModel>(json);

                if (message is not null)
                    await onMessageReceived(message);
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
                await _channel.CloseAsync();

            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}
