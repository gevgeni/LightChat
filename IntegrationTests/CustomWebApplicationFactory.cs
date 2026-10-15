using LightChat.Infrastructure.Persistence;

using Testcontainers.PostgreSql;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LightChat.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("lightchat_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public string DbConnectionString => _dbContainer.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        // ВАЖНО: этот метод вызывается ДО запуска Program.Main.
        // Env vars успевают попасть в WebApplicationBuilder.Configuration,
        // поэтому Program.cs увидит уже непустые значения.
        protected override IHost CreateHost(IHostBuilder builder)
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__PostgresConnection",
                _dbContainer.GetConnectionString());

            Environment.SetEnvironmentVariable(
                "ConnectionStrings__RedisConnection",
                "localhost:6379");

            Environment.SetEnvironmentVariable(
                "JwtSettings__Secret",
                "integration-tests-secret-key-at-least-32-bytes!!");

            Environment.SetEnvironmentVariable(
                "JwtSettings__Issuer", "LightChatBackend");

            Environment.SetEnvironmentVariable(
                "JwtSettings__Audience", "LightChatClient");

            Environment.SetEnvironmentVariable(
                "RabbitMq__Host", "localhost");

            Environment.SetEnvironmentVariable(
                "RabbitMq__Username", "guest");

            Environment.SetEnvironmentVariable(
                "RabbitMq__Password", "guest");

            Environment.SetEnvironmentVariable(
                "Vapid__Subject", "mailto:test@test.local");

            Environment.SetEnvironmentVariable(
                "Vapid__PublicKey", "test-public-key");

            Environment.SetEnvironmentVariable(
                "Vapid__PrivateKey", "test-private-key");

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString());
                });
            });
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _dbContainer.StopAsync();
        }
    }
}