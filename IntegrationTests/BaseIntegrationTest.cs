using LightChat.Infrastructure.Persistence;

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
    }
}