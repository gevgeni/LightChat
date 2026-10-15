using LightChat.Core.Features.Users.UserJwtAuthorize;
using LightChat.Infrastructure.Persistence;
using LightChat.Web.Requests;
using Microsoft.EntityFrameworkCore;

namespace LightChat.IntegrationTests
{
    [Collection("IntegrationTests")]
    public abstract class BaseIntegrationTest
    {
        protected readonly HttpClient Client;
        protected readonly IServiceScope Scope;
        protected readonly ApplicationDbContext DbContext;
        private readonly IServiceScopeFactory _scopeFactory;
        

        protected BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
            Client = factory.CreateClient();
            Scope = factory.Services.CreateScope();
            DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        protected async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action) 
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await action(dbContext);
        }

        protected async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await action(dbContext);
        }

        protected async Task<(string Token, Guid userId)> RegisterAndLoginAsync(string username, string email)
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
    }
}