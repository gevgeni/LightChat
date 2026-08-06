using System.Net;

using FluentAssertions;

using LightChat.Web.Requests;
using LightChat.Core.Features.Users.UserJwtAuthorize;
using LightChat.Core.Entities;

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
            var request = new CreateUserRequest("integration_user", "test@chat.com", "Password123!");

            var response = await Client.PostAsJsonAsync("/users", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            User? userInDb = null;
            await ExecuteDbContextAsync(async db => userInDb = db.Users.FirstOrDefault(u => u.Username == request.Username));

            userInDb.Should().NotBeNull();
            userInDb.Email.Should().Be(request.Email);
        }

        [Fact]
        public async Task Login_Should_ReturnJwtToken_When_CredentialsAreValid()
        {
            var registerRequest = new CreateUserRequest("auth_user", "auth@chat.com", "Password123!");

            await Client.PostAsJsonAsync("/users", registerRequest);

            var loginRequest = new LoginRequest(registerRequest.Username, registerRequest.Password);

            var response = await Client.PostAsJsonAsync("/auth/login", loginRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<JwtTokenDto>();
            result.Should().NotBeNull();
            result.TokenString.Should().NotBeNullOrEmpty();
        }
    }
}