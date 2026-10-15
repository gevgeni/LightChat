using LightChat.Core.Features.Users.UserJwtAuthorize;
using LightChat.Infrastructure.Persistence;
using LightChat.Web.Requests;

using Microsoft.EntityFrameworkCore;

using Npgsql;
using Respawn;
using Respawn.Graph;

namespace LightChat.IntegrationTests
{
    [Collection("IntegrationTests")]
    public abstract class BaseIntegrationTest
    {
        protected readonly HttpClient Client;
        private readonly CustomWebApplicationFactory Factory;
        private readonly IServiceScopeFactory _scopeFactory;

        private static Respawner? _respawner;
        private static readonly SemaphoreSlim _respawnerLock = new(1, 1);

        protected BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            Factory = factory;
            _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
            Client = factory.CreateClient();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            var respawner = await GetRespawnerAsync();

            await using var conn = new NpgsqlConnection(Factory.DbConnectionString);
            await conn.OpenAsync();
            await respawner.ResetAsync(conn);
        }

        private async Task<Respawner> GetRespawnerAsync()
        {
            if (_respawner is not null)
                return _respawner;

            await _respawnerLock.WaitAsync();
            try
            {
                if (_respawner is not null)
                    return _respawner;

                await using var conn = new NpgsqlConnection(Factory.DbConnectionString);
                await conn.OpenAsync();

                _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = new[] { "public" },
                    TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
                });

                return _respawner;
            }
            finally
            {
                _respawnerLock.Release();
            }
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