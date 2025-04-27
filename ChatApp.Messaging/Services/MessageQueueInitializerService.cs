using ChatApp.Messaging.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ChatApp.Messaging.Services
{
    public class MessageQueueInitializerService : IHostedService
    {
        private readonly IMessageQueueService _messageQueueService;

        public MessageQueueInitializerService(IMessageQueueService messageQueueService)
        {
            _messageQueueService = messageQueueService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageQueueService.InitializeAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_messageQueueService is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
    }
}
