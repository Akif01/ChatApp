using ChatApp.Core.Models;

namespace ChatApp.Messaging.Interfaces
{
    public interface IMessageQueueService
    {
        Task InitializeAsync();
        Task PublishAsync(ChatMessageModel message);
        Task StartConsumingAsync(Func<ChatMessageModel, Task> onMessageReceived);
    }
}
