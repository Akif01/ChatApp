using System.Text;
using System.Text.Json;
using ChatApp.Core.Models;
using ChatApp.Messaging.Configuration;
using ChatApp.Messaging.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApp.Messaging.Services
{
    public class RabbitMqService : IMessageQueueService, IAsyncDisposable
    {
        private IConnection _connection = null!;
        private IChannel _channel = null!;
        private readonly RabbitMqOptions _options = null!;
        private string _exchangeName = "";

        public RabbitMqService(IOptions<RabbitMqOptions> options)
        {
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

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Fanout,
                durable: false,
                autoDelete: false);
        }

        public async Task PublishAsync(ChatMessageModel message)
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ not initialized");

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
            if (_channel is null) 
                throw new InvalidOperationException("RabbitMQ not initialized");

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
            await _channel.CloseAsync();
            await _connection.CloseAsync();
        }
    }
}
