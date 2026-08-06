using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;

using LightChat.Web.Requests;
using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Features.Users.UserJwtAuthorize;
using Microsoft.EntityFrameworkCore;

namespace LightChat.IntegrationTests.Endpoints
{
    public class ChatCreateEndpointsTests : BaseIntegrationTest
    {
        public ChatCreateEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateChatRequest_Should_ReturnNewChat_When_RequestIsValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var loginRequest = new LoginRequest("auth_user", "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            var token = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            var request = new CreateChatRequest("new_chat");

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.TokenString);

            var response = await Client.PostAsJsonAsync("/chats", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var result = await response.Content.ReadFromJsonAsync<ChatResultDto>();

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Name.Should().Be("new_chat");
            result.IsDirect.Should().BeFalse();
        }

        [Fact]
        public async Task CreateDirectChatRequest_Should_ReturnNewChat_When_RequestIsValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var companionRegisterRequest = new CreateUserRequest("companion", "companion@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", companionRegisterRequest);

            var loginRequest = new LoginRequest("auth_user", "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            var token = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            Guid companionId = Guid.Empty;
            Guid userId = Guid.Empty;
            await ExecuteDbContextAsync(async db =>
            {
                companionId = (await db.Users.FirstAsync(u => u.Username == companionRegisterRequest.Username)).Id;
                userId = (await db.Users.FirstAsync(u => u.Username == registerRequest.Username)).Id;
            });

            var createDirectChatRequest = new CreateDirectChatRequest(companionId);

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.TokenString);

            var response = await Client.PostAsJsonAsync("/chats/direct", createDirectChatRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var result = await response.Content.ReadFromJsonAsync<ChatResultDto>();

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Name.Should().Be("DM");
            result.IsDirect.Should().BeTrue();
        }

        [Fact]
        public async Task AddChatMemberRequest_Should_AddMemberInChat_When_RequestIsValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var companionRegisterRequest = new CreateUserRequest("companion", "companion@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", companionRegisterRequest);

            var loginRequest = new LoginRequest("auth_user", "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            var token = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.TokenString);

            Guid companionId = Guid.Empty;
            Guid userId = Guid.Empty;
            await ExecuteDbContextAsync(async db =>
            {
                companionId = (await db.Users.FirstAsync(u => u.Username == companionRegisterRequest.Username)).Id;
                userId = (await db.Users.FirstAsync(u => u.Username == registerRequest.Username)).Id;
            });

            var createChatRequest = new CreateChatRequest("new_chat");
            var createChatResponse = await Client.PostAsJsonAsync("/chats", createChatRequest);
            createChatResponse.EnsureSuccessStatusCode();

            Guid chatId = Guid.Empty;
            await ExecuteDbContextAsync(async db => chatId = (await db.Chats.FirstAsync(c => c.Name == createChatRequest.Name)).Id);

            var request = new AddMemberRequest(companionId);

            var response = await Client.PostAsJsonAsync($"/chats/{chatId}/members", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await ExecuteDbContextAsync(async db =>
            {
                var chatInDb = await db.Chats
                    .Include(c => c.ChatMembers)
                    .FirstAsync(c => c.Id == chatId);

                chatInDb.ChatMembers.Should().HaveCount(2);
                chatInDb.ChatMembers.Last().UserId.Should().Be(companionId);
            });
        }
    }
}
