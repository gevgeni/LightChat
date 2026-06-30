using Microsoft.EntityFrameworkCore;

using LightChat.Web.Hubs;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;
using LightChat.Infrastructure.Repositories;
using LiteChat.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options => options.EnableDetailedErrors = true);

#region Настройка PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
#endregion

#region Регистрация репозиториев
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IChatRepository, EfChatRepository>();
builder.Services.AddScoped<IMessageRepository, EfMessageRepository>();
#endregion

builder.Services.AddOpenApi();

var app = builder.Build();

#region Формирование миграций при старте
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
#endregion

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

#region Minimal API Эндпоинты
app.MapPost("/api/users", async (CreateUserDto dto, IUserRepository userRepo) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username))
        return Results.BadRequest("Имя пользователя не может быть пустым.");

    if (string.IsNullOrWhiteSpace(dto.Email))
        return Results.BadRequest("Email не может быть пустым.");

    var existingUser = await userRepo.GetByUsernameAsync(dto.Username);
    if (existingUser != null)
        return Results.Conflict("Пользователь с таким ником уже существует.");

    var user = new LightChat.Core.Entities.User
    {
        Id = Guid.NewGuid(),
        Username = dto.Username,
        Email = dto.Email,
        CreatedAt = DateTime.UtcNow
    };

    await userRepo.CreateAsync(user);
    return Results.Ok(user);
});

app.MapPost("/api/chats", async (CreateChatDto dto, IChatRepository chatRepo) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest("Название чата не может быть пустым.");

    var chat = new LightChat.Core.Entities.Chat
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        CreatedAt = DateTime.UtcNow
    };

    await chatRepo.CreateAsync(chat);
    return Results.Ok(chat);
});

app.MapPost("/api/chats/members", async (AddMemberDto dto, IChatRepository chatRepo, IUserRepository userRepo) =>
{
    var userExists = await userRepo.ExistsAsync(dto.UserId);
    if (!userExists)
        return Results.NotFound("Пользователь не найден.");

    var chat = await chatRepo.GetByIdAsync(dto.ChatId);
    if (chat == null)
        return Results.NotFound("Чат не найден.");

    var isAlreadyMember = await chatRepo.IsMemberAsync(dto.ChatId, dto.UserId);
    if (isAlreadyMember)
        return Results.Conflict("Пользователь уже состоит в этом чате.");

    var member = new LightChat.Core.Entities.ChatMember
    {
        ChatId = dto.ChatId,
        UserId = dto.UserId,
        JoinedAt = DateTime.UtcNow
    };

    await chatRepo.AddMemberAsync(member);
    return Results.Ok("Пользователь успешно добавлен в чат.");
});
#endregion

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapHub<ChatHub>("/chatHub");

app.Run();

#region DTO
public record CreateUserDto(string Username, string Email);
public record CreateChatDto(string Name);
public record AddMemberDto(Guid ChatId, Guid UserId);
#endregion