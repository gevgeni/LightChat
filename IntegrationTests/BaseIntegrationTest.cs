using LightChat.Infrastructure.Persistence;

namespace LightChat.IntegrationTests
{
    [Collection("IntegrationTests")]
    public abstract class BaseIntegrationTest
    {
        protected readonly HttpClient Client;
        protected readonly IServiceScope Scope;
        protected readonly ApplicationDbContext DbContext;

        protected BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            Client = factory.CreateClient();
            Scope = factory.Services.CreateScope();
            DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
    }
}