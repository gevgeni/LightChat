using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using LightChat.Web.Requests;
using LightChat.Core.Entities;
using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Features.Messages.GetMessageHistory;
using LightChat.Core.Features.Users.UserJwtAuthorize;

namespace LightChat.IntegrationTests.Endpoints
{
    public class MessageGetEndpointsTests : BaseIntegrationTest
    {
        public MessageGetEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetMessageHistory_Should_ReturnChatsMessages_When_RequestIsValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var loginQuery = new LoginRequest("auth_user", "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginQuery);
            var token = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            Guid userId = Guid.Empty;
            await ExecuteDbContextAsync(async db => userId = (await db.Users.FirstAsync(u => u.Username == registerRequest.Username)).Id);

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.TokenString);

            var createChatCommand = new CreateChatRequest("new_chat");
            var createChatResponse = await Client.PostAsJsonAsync("/chats", createChatCommand);
            var chatInDb = await createChatResponse.Content.ReadFromJsonAsync<ChatResultDto>();

            await ExecuteDbContextAsync(async db =>
            {
                var messages = new List<Message>
                {
                    new() { Id = Guid.NewGuid(), ChatId = chatInDb!.Id, SenderId = userId, Text = "First Message", SentAt = DateTime.UtcNow.AddMinutes(-2) },
                    new() { Id = Guid.NewGuid(), ChatId = chatInDb.Id, SenderId = userId, Text = "Second Message", SentAt = DateTime.UtcNow.AddMinutes(-1) }
                };

                await db.Messages.AddRangeAsync(messages);
                await db.SaveChangesAsync();
            });

            var response = await Client.GetAsync($"/chats/{chatInDb!.Id}/messages?limit=10");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<MessageDto>>();
            result.Should().NotBeNullOrEmpty();
            result.Should().HaveCount(2);
        }
    }
}