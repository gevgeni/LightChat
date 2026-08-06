using System.Net.Http.Headers;

using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR.Client;

using LightChat.Web.Requests;
using LightChat.Core.Entities;
using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Features.Users.UserJwtAuthorize;

namespace LightChat.IntegrationTests.Hubs
{
    public class ChatHubTests : BaseIntegrationTest
    {
        private readonly CustomWebApplicationFactory Factory;
        public ChatHubTests(CustomWebApplicationFactory factory) : base(factory)
        {
            Factory = factory;
        }

        private HubConnection CreateHubConnection(string token)
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri(Factory.Server.BaseAddress, "/chatHub"), options =>
                {
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();
        }

        private async Task<(string Token, Guid userId)> RegisterAndLoginAsync(string username, string email)
        {
            var registerRequest = new CreateUserRequest(username, email, "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var loginRequest = new LoginRequest(username, "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            var tokenDto = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            Guid userId = Guid.Empty;
            await ExecuteDbContextAsync(async db => userId = (await db.Users.FirstAsync(u => u.Username == username)).Id);

            return (tokenDto!.TokenString, userId);
        }

        [Fact]
        public async Task Connection_Should_BroadcastUserStatusChanged_OnConnectAndDisconnect()
        {
            var (user1Token, user1Id) = await RegisterAndLoginAsync("status_user1", "status1@chat.com");
            var (user2Token, user2Id) = await RegisterAndLoginAsync("status_user2", "status2@chat.com");

            await using var connection1 = CreateHubConnection(user1Token);
            await connection1.StartAsync();

            var statusTask = new TaskCompletionSource<UserStatusNotification>();
            connection1.On<UserStatusNotification>("UserStatusChanged", status =>
            {
                if (status.UserId == user2Id && status.IsOnline)
                    statusTask.TrySetResult(status);
            });

            await using var connection2 = CreateHubConnection(user2Token);
            await connection2.StartAsync();

            var connectResult = await Task.WhenAny(statusTask.Task, Task.Delay(3000));
            connectResult.Should().Be(statusTask.Task, "Уведомление об онлайн-статусе user2 должно прийти");

            var onlineStatus = await statusTask.Task;
            onlineStatus.UserId.Should().Be(user2Id);
            onlineStatus.IsOnline.Should().BeTrue();

            var disconnectStatusTask = new TaskCompletionSource<UserStatusNotification>();
            connection1.On<UserStatusNotification>("UserStatusChanged", status =>
            {
                if (status.UserId == user2Id && !status.IsOnline)
                    disconnectStatusTask.TrySetResult(status);
            });

            await connection2.StopAsync();

            var disconnectResult = await Task.WhenAny(disconnectStatusTask.Task, Task.Delay(3000));
            disconnectResult.Should().Be(disconnectStatusTask.Task, "Уведомление об офлайн-статусе user2 должно прийти");

            var offlineStatus = await disconnectStatusTask.Task;
            offlineStatus.UserId.Should().Be(user2Id);
            offlineStatus.IsOnline.Should().BeFalse();
        }

        [Fact]
        public async Task JoinChat_Should_ThrowException_When_UserIsNotMember()
        {
            var (user1Token, _) = await RegisterAndLoginAsync("owner_user", "owner@chat.com");
            var (user2Token, _) = await RegisterAndLoginAsync("outsider_user", "outsider@chat.com");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
            var createChatResponse = await Client.PostAsJsonAsync("/chats", new CreateChatRequest("private_chat"));
            var chat = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await using var connection2 = CreateHubConnection(user2Token);
            await connection2.StartAsync();

            Func<Task> act = async () => await connection2.InvokeAsync("JoinChat", chat!.Id);

            await act.Should().ThrowAsync<HubException>()
                .WithMessage("*Вы не являетесь участником этого чата*");
        }

        [Fact]
        public async Task LeaveChat_Should_RemoveUserFromGroupAndAuthorizedChats()
        {
            var (token, _) = await RegisterAndLoginAsync("leave_user", "leave@chat.com");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createChatResponse = await Client.PostAsJsonAsync("/chats", new CreateChatRequest("leave_chat"));
            var chat = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await using var connection = CreateHubConnection(token);
            await connection.StartAsync();

            await connection.InvokeAsync("JoinChat", chat!.Id);
            await connection.InvokeAsync("LeaveChat", chat.Id);

            Func<Task> act = async () => await connection.InvokeAsync("SendMessage", chat.Id, "Test message");

            await act.Should().ThrowAsync<HubException>()
                .WithMessage("*Вы не являетесь участником этого чата*");
        }

        [Fact]
        public async Task JoinAndSendMessage_Should_BroadcastMessageToChatMembers()
        {
            var (token, userId) = await RegisterAndLoginAsync("sender_user", "sender@chat.com");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createChatResponse = await Client.PostAsJsonAsync("/chats", new CreateChatRequest("hub_chat"));
            var chat = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await using var hubConnection = CreateHubConnection(token);

            var receivedMessageTask = new TaskCompletionSource<ReceiveMessageNotification>();
            hubConnection.On<ReceiveMessageNotification>("ReceiveMessage", receivedMessageTask.SetResult);

            await hubConnection.StartAsync();

            await hubConnection.InvokeAsync("JoinChat", chat!.Id);
            await hubConnection.InvokeAsync("SendMessage", chat!.Id, "Hello SignalR!");

            var completedTask = await Task.WhenAny(receivedMessageTask.Task, Task.Delay(3000));
            completedTask.Should().Be(receivedMessageTask.Task, "Сообщение должно прийти в течение 3 секунд");

            var receivedMessage = await receivedMessageTask.Task;
            receivedMessage.Text.Should().Be("Hello SignalR!");
            receivedMessage.SenderUsername.Should().Be("sender_user");

            await ExecuteDbContextAsync(async db =>
            {
                var messageInDb = await db.Messages.FirstOrDefaultAsync(m => m.ChatId == chat.Id);
                messageInDb.Should().NotBeNull();
                messageInDb!.Text.Should().Be("Hello SignalR!");
            });
        }

        [Fact]
        public async Task SendMessage_Should_ThrowException_When_UserHasNotJoinedChat()
        {
            var (token, _) = await RegisterAndLoginAsync("unjoined_user", "unjoined@chat.com");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createChatResponse = await Client.PostAsJsonAsync("/chats", new CreateChatRequest("secret_chat"));
            var chat = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await using var hubConnection = CreateHubConnection(token);
            await hubConnection.StartAsync();

            Func<Task> act = async () => await hubConnection.InvokeAsync("SendMessage", chat!.Id, "Fail Message");
            await act.Should().ThrowAsync<HubException>()
                .WithMessage("*Вы не являетесь участником этого чата*");
        }

        [Fact]
        public async Task NotifyTyping_Should_SendNotificationToOtherMembers()
        {
            var (user1Token, user1Id) = await RegisterAndLoginAsync("user1", "user1@chat.com");
            var (user2Token, user2Id) = await RegisterAndLoginAsync("user2", "user2@chat.com");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
            var createChatResponse = await Client.PostAsJsonAsync("/chats", new CreateChatRequest("typing_chat"));
            var chat = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await Client.PostAsJsonAsync($"/chats/{chat!.Id}/members", new AddMemberRequest(user2Id));

            await using var connection1 = CreateHubConnection(user1Token);
            await using var connection2 = CreateHubConnection(user2Token);

            await connection1.StartAsync();
            await connection2.StartAsync();

            await connection1.InvokeAsync("JoinChat", chat.Id);
            await connection2.InvokeAsync("JoinChat", chat.Id);

            var typingTask = new TaskCompletionSource<Guid>();
            connection2.On<UserTypingNotification>("UserIsTyping", notification =>
                typingTask.SetResult(notification.UserId));

            await connection1.InvokeAsync("NotifyTyping", chat.Id);

            var completedTask = await Task.WhenAny(typingTask.Task, Task.Delay(3000));
            completedTask.Should().Be(typingTask.Task);

            var notifiedUserId = await typingTask.Task;
            notifiedUserId.Should().Be(user1Id);
        }

        [Fact]
        public async Task MarkChatAsRead_Should_UpdateMessagesAndBroadcastEvent()
        {
            var (token, userId) = await RegisterAndLoginAsync("reader_user", "reader@chat.com");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createChatResponse = await Client.PostAsJsonAsync("/chats", new CreateChatRequest("read_chat"));
            var chat = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await ExecuteDbContextAsync(async db =>
            {
                db.Messages.Add(new Message
                {
                    Id = Guid.NewGuid(),
                    ChatId = chat!.Id,
                    SenderId = userId,
                    Text = "Unread message",
                    SentAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            });

            await using var hubConnection = CreateHubConnection(token);
            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("JoinChat", chat!.Id);

            var readTask = new TaskCompletionSource<MessageReadingNotification>();
            hubConnection.On<MessageReadingNotification>("MessagesMarkedAsRead", readTask.SetResult);

            await hubConnection.InvokeAsync("MarkChatAsRead", chat.Id);

            var completedTask = await Task.WhenAny(readTask.Task, Task.Delay(3000));
            completedTask.Should().Be(readTask.Task);

            var (readChatId, readerId) = await readTask.Task;
            readChatId.Should().Be(chat.Id);
            readerId.Should().Be(userId);
        }
    }

    public record UserTypingNotification(Guid UserId, Guid ChatId);
    public record MessageReadingNotification(Guid ChatId, Guid ReaderId);
    public record UserStatusNotification(Guid UserId, bool IsOnline);
    public record ReceiveMessageNotification(
        Guid Id,
        Guid ChatId,
        Guid SenderId,
        string SenderUsername,
        string Text,
        DateTime SentAt,
        bool IsRead
    );
}
