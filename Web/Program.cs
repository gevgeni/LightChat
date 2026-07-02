using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using LightChat.Web.Hubs;
using LightChat.Web.Models;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;
using LightChat.Infrastructure.Repositories;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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

#region JWT авторизация
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? string.Empty);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});
#endregion

builder.Services.AddAuthentication();

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

//endpoint - авторизация пользователя с Json Web Token
app.MapPost("/auth/login", async (LoginRequest request, ApplicationDbContext dbContext, IConfiguration configuration) =>
{
    if (string.IsNullOrWhiteSpace(request.Username))
        return Results.BadRequest("Имя пользователя не может быть пустым.");

    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null)
        return Results.Unauthorized();

    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? string.Empty);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

    var key = new SymmetricSecurityKey(secretKey);
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddDays(1),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = creds
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var securityToken = tokenHandler.CreateToken(token);
    var tokenString = tokenHandler.WriteToken(securityToken);

    return Results.Ok(new { Token = tokenString } );
});

app.MapGet("chats/{chatId:guid}/messages", async (
    Guid chatId,
    int limit,
    Guid? beforeMessageId,
    IChatRepository chatRepository,
    IMessageRepository messageRepository,
    IUserRepository userRepository,
    ClaimsPrincipal user) =>
{
    var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        return Results.Unauthorized();

    var isMember = await chatRepository.IsMemberAsync(chatId, userId);
    if (!isMember)
        return Results.Forbid();

    var effectiveLimit = limit > 0 ? limit : 50;
    var messages = await messageRepository.GetChatHistoryAsync(chatId, effectiveLimit, beforeMessageId);

    var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
    var usernamesDict = new Dictionary<Guid, string>();

    foreach (var id in senderIds)
    {
        var u = await userRepository.GetByIdAsync(id);
        if (u != null) usernamesDict[id] = u.Username;
    }

    var result = messages.Select(m => new
    {
        id = m.Id,
        chatId = m.ChatId,
        senderId = m.SenderId,
        senderUsername = usernamesDict.TryGetValue(m.SenderId, out var name) ? name : "Неизвестный",
        text = m.Text,
        sentAt = m.SentAt
    });

    return Results.Ok(result);
})
.RequireAuthorization();

app.MapGet("/chats", async (
    IChatRepository chatRepo,
    ClaimsPrincipal user) =>
{
    var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        return Results.Unauthorized();

    var userChats = await chatRepo.GetUserChatsAsync(userId);

    var result = userChats.Select(c => new
    {
        id = c.Id,
        name = c.Name,
        createdAt = c.CreatedAt
    });

    return Results.Ok(result);
})
.RequireAuthorization();

app.MapPost("/chats", async (CreateChatDto dto, IChatRepository chatRepository, ClaimsPrincipal user) =>
{
    var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest("Название чата не может быть пустым.");

    var chat = new LightChat.Core.Entities.Chat
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        CreatedAt = DateTime.UtcNow
    };

    var member = new LightChat.Core.Entities.ChatMember
    {
        ChatId = chat.Id,
        UserId = userId,
        JoinedAt = DateTime.UtcNow
    };

    await chatRepository.CreateGroupChatAsync(chat, member);
    return Results.Ok(new
    {
        id = chat.Id,
        name = chat.Name,
        createdAt = chat.CreatedAt
    });
});

app.MapGet("/chats/{chatId}/members", async (Guid chatId, IChatRepository chatRepository, HttpContext context) =>
{
    var membersAsUsers = await chatRepository.GetMembersAsync(chatId);

    var results = membersAsUsers.Select(u => new
    {
        id =u.Id,
        username = u.Username,
        email = u.Email
    });

    return Results.Ok(results);
})
.RequireAuthorization();

//endpoint - добавление участников в чат
app.MapPost("/chats/{chatId:guid}/members", async (Guid chatId, AddMemberDto dto, IChatRepository chatRepository, ClaimsPrincipal user) =>
{
    var chat = await chatRepository.GetByIdAsync(chatId);
    if (chat == null)
        return Results.NotFound("Чат не найден.");

    var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        return Results.Unauthorized();

    var isCurrentMember = await chatRepository.IsMemberAsync(chatId, userId);
    if (!isCurrentMember)
        return Results.Forbid();

    var isAlreadyMember = await chatRepository.IsMemberAsync(chatId, dto.UserId);
    if (isAlreadyMember)
        return Results.Conflict("Пользователь уже состоит в этом чате.");

    var member = new LightChat.Core.Entities.ChatMember
    {
        ChatId = chatId,
        UserId = dto.UserId,
        JoinedAt = DateTime.UtcNow
    };

    await chatRepository.AddMemberAsync(member);
    return Results.Ok("Пользователь успешно добавлен в чат.");
})
.RequireAuthorization();

//endpoint - получение всех пользователей
app.MapGet("/users", async (IUserRepository userRepository, ClaimsPrincipal user) =>
{
    var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var currentUserId))
        return Results.Unauthorized();

    var allUsers = await userRepository.GetAllAsync();

    var result = allUsers
        .Where(u => u.Id != currentUserId)
        .Select(u => new
        {
            id = u.Id,
            username = u.Username
});


#endregion

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");

app.Run();