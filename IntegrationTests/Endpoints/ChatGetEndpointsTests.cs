using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using LightChat.Web.Requests;
using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Features.Chats.GetChatMembers;
using LightChat.Core.Features.Users.UserJwtAuthorize;

namespace LightChat.IntegrationTests.Endpoints
{
    public class ChatGetEndpointsTests : BaseIntegrationTest
    {
        public ChatGetEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetUserChats_Should_ReturnUserChats_When_RequestIsValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var loginRequest = new LoginRequest("auth_user", "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            var token = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.TokenString);

            Guid userId = Guid.Empty;
            await ExecuteDbContextAsync(async db => userId = (await db.Users.FirstAsync(u => u.Username == registerRequest.Username)).Id);

            var createChatRequest = new CreateChatRequest("new_chat");
            var createChatResponse = await Client.PostAsJsonAsync("/chats", createChatRequest);
            createChatResponse.EnsureSuccessStatusCode();

            Guid chatId = Guid.Empty;
            await ExecuteDbContextAsync(async db => chatId = (await db.Chats.FirstAsync(c => c.Name == createChatRequest.Name)).Id);

            var response = await Client.GetAsync($"/chats");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<ChatResultDto>>();

            result.Should().NotBeNullOrEmpty();
            result.Should().HaveCountGreaterThanOrEqualTo(1);
            result.Should().Contain(m => m.Id == chatId);
        }

        [Fact]
        public async Task GetChatMembers_Should_ReturnChatMembers_When_RequestIsValid()
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

            var addMemberRequest = new AddMemberRequest(companionId);

            await Client.PostAsJsonAsync($"/chats/{chatId}/members", addMemberRequest);

            var response = await Client.GetAsync($"/chats/{chatId}/members");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<ChatMemberDto>>();

            result.Should().NotBeNullOrEmpty();
            result.Should().HaveCountGreaterThanOrEqualTo(2);
            result.Should().Contain(m => m.Id == userId);
            result.Should().Contain(m => m.Id == companionId);
        }
    }
}