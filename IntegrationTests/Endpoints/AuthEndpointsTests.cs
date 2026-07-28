using System.Net;
using FluentAssertions;
using LightChat.Core.Features.Users.UserRegister;
using LightChat.Core.Features.Users.UserJwtAuthorize;
using LightChat.Web.Requests;

namespace LightChat.IntegrationTests.Endpoints
{
    public class AuthEndpointsTests : BaseIntegrationTest
    {
        public AuthEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Register_Should_CreateUserInDatabase_When_RequestIsValid()
        {
            var command = new CreateUserRequest("integration_user", "test@chat.com", "Password123!");

            var response = await Client.PostAsJsonAsync("/users", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var userInDb = DbContext.Users.FirstOrDefault(u => u.Username == command.Username);
            userInDb.Should().NotBeNull();
            userInDb.Email.Should().Be(command.Email);
        }

        [Fact]
        public async Task Login_Should_ReturnJwtToken_When_CredentialsAreValid()
        {
            var registerCommand = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");

            await Client.PostAsJsonAsync("/users", registerCommand);

            var loginQuery = new LoginRequest(registerCommand.Username, registerCommand.Password);

            var response = await Client.PostAsJsonAsync("/auth/login", loginQuery);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result.TokenString.Should().NotBeNullOrEmpty();
        }

        private record AuthResponseDto(string TokenString);
    }
}