using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using LightChat.Web.Requests;
using LightChat.Core.Features.Users.GetAllUsers;
using LightChat.Core.Features.Users.UserJwtAuthorize;

namespace LightChat.IntegrationTests.Endpoints
{
    public class UserGetEndpointsTests : BaseIntegrationTest
    {
        public UserGetEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAllUser_Should_ReturnUsers_When_RequestIsValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", registerRequest);

            var userRegisterRequest1 = new CreateUserRequest("companion", "companion@chat.com", "Password123!");
            var userRegisterRequest2 = new CreateUserRequest("companion2", "companion2@chat.com", "Password123!");
            await Client.PostAsJsonAsync("/users", userRegisterRequest1);
            await Client.PostAsJsonAsync("/users", userRegisterRequest2);

            var loginRequest = new LoginRequest("auth_user", "Password123!");
            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            var token = await loginResponse.Content.ReadFromJsonAsync<JwtTokenDto>();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.TokenString);

            Guid user1Id = Guid.Empty;
            Guid user2Id = Guid.Empty;
            Guid userId = Guid.Empty; 

            await ExecuteDbContextAsync(async db =>
            {
                user1Id = (await db.Users.FirstAsync(u => u.Username == userRegisterRequest1.Username)).Id;
                user2Id = (await db.Users.FirstAsync(u => u.Username == userRegisterRequest2.Username)).Id;
                userId = (await db.Users.FirstAsync(u => u.Username == registerRequest.Username)).Id;
            });

            var response = await Client.GetAsync("/users");
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<UserDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Should().NotBeNullOrEmpty();
            result.Should().HaveCountGreaterThanOrEqualTo(2);
            result.Should().Contain(m => m.Id == user1Id);
            result.Should().Contain(m => m.Id == user2Id);
        }
    }
}