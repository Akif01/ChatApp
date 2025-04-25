using ChatApp.Messaging.Interfaces;
using ChatApp.Messaging.Services;
using Microsoft.Extensions.Hosting;

namespace ChatApp.Messaging.Services
{
    public class RabbitMqInitializerService : IHostedService
    {
        private readonly RabbitMqService _mqService;

        public RabbitMqInitializerService(IMessageQueueService mqService)
        {
            _mqService = (RabbitMqService)mqService; // Cast to call InitializeAsync
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _mqService.InitializeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
