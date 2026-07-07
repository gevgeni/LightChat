using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Serilog;
using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using LightChat.Web.Hubs;
using LightChat.Web.Models;
using LightChat.Core.Entities;
using LightChat.Web.Middlwares;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;
using LightChat.Infrastructure.Repositories;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/lightchat-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Запуск веб-приложения LightChat...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddSignalR(options => options.EnableDetailedErrors = true);

    builder.Services.AddOpenApi();

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddExceptionHandler<CustomExceptionHandler>();
    builder.Services.AddProblemDetails();

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

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    #region Формирование миграций при старте
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var legacyUsers = await dbContext.Users
            .Where(u => u.PasswordHash == string.Empty)
            .ToListAsync();

        if (legacyUsers.Count != 0)
        {
            string defaultHash = BCrypt.Net.BCrypt.HashPassword("123456");

            foreach (var user in legacyUsers)
                user.PasswordHash = defaultHash;

            await dbContext.SaveChangesAsync();
            Console.WriteLine($"[Migration] Успешно обновлено паролей для {legacyUsers.Count} старых пользователей. Дефолтный пароль: 123456");
        }
    }
    #endregion

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    #region Minimal API Эндпоинты
    //endpoint - регистрация пользователя
    app.MapPost("/api/users", async (CreateUserDto dto, IValidator<CreateUserDto> validator, IUserRepository userRepo) =>
    {
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var existingUser = await userRepo.GetByUsernameAsync(dto.Username);
        if (existingUser != null)
            return Results.Conflict("Пользователь с таким ником уже существует.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        await userRepo.CreateAsync(user);
        return Results.Ok(new { user.Id, user.Username });
    });

    //endpoint - авторизация пользователя с Json Web Token
    app.MapPost("/auth/login", async (LoginRequest request, IValidator<LoginRequest> validator, ApplicationDbContext dbContext, IConfiguration configuration) =>
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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

        return Results.Ok(new { Token = tokenString });
    });

    //endpoint - получение истории сообщений
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
        List<User> explicitUsers = await userRepository.GetAllContainsInIdsAsync(senderIds);

        var usernamesDict = explicitUsers.ToDictionary(u => u.Id, u => u.Username);

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

    //endpoint - получение всех чатов пользователя
    app.MapGet("/chats", async (
        IChatRepository chatRepo,
        ClaimsPrincipal user) =>
    {
        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            return Results.Unauthorized();

        var userChats = await chatRepo.GetUserChatsAsync(userId);
        var resultList = new List<object>();

        foreach (var c in userChats)
        {
            string finalName = c.Name;

            if (c.IsDirect)
            {
                var members = await chatRepo.GetMembersAsync(c.Id);
                var companion = members.FirstOrDefault(m => m.Id != userId);
                finalName = companion != null ? companion.Username : "Удаленный пользователь";
            }

            resultList.Add(new
            {
                id = c.Id,
                name = finalName,
                isDirect = c.IsDirect,
                createdAt = c.CreatedAt
            });
        }

        return Results.Ok(resultList);
    })
    .RequireAuthorization();

    //endpoint - создание групового чата
    app.MapPost("/chats", async (CreateChatDto dto, IValidator<CreateChatDto> validator, IChatRepository chatRepository, ClaimsPrincipal user) =>
    {
        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            return Results.Unauthorized();

        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };

        var member = new ChatMember
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
    })
    .RequireAuthorization();

    //endpoint - создание личного чата
    app.MapPost("/chats/direct", async (CreateDirectChatRequest request, IChatRepository chatRepository, ClaimsPrincipal principal) =>
    {
        var currentUserIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(currentUserIdString, out var currentUserId)) return Results.Unauthorized();

        var targetUserId = request.TargetUserId;
        if (targetUserId == currentUserId) return Results.BadRequest("Нельзя создать личный чат с самим собой.");

        var existingChat = await chatRepository.GetDirectChatAsync(currentUserId, targetUserId);
        if (existingChat != null)
            return Results.Ok(new { id = existingChat.Id, name = "Личный чат", isDirect = true });

        var newChat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = "DM",
            IsDirect = true,
            CreatedAt = DateTime.UtcNow
        };

        var currentUser = new ChatMember { ChatId = newChat.Id, UserId = currentUserId, JoinedAt = DateTime.UtcNow };
        var targetUser = new ChatMember { ChatId = newChat.Id, UserId = targetUserId, JoinedAt = DateTime.UtcNow };

        await chatRepository.CreateDirectChatAsync(newChat, currentUser, targetUser);

        return Results.Ok(new { id = newChat.Id, name = "Личный чат", isDirect = true });
    })
    .RequireAuthorization();

    //endpoint - получение участников чата
    app.MapGet("/chats/{chatId}/members", async (Guid chatId, IChatRepository chatRepository, HttpContext context) =>
    {
        var membersAsUsers = await chatRepository.GetMembersAsync(chatId);

        var results = membersAsUsers.Select(u => new
        {
            id = u.Id,
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

        if (chat.IsDirect)
            return Results.BadRequest("В личный чат нельзя приглашать сторонних участников.");

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

        return Results.Ok(result);
    })
    .RequireAuthorization();
    #endregion

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<ChatHub>("/chatHub");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение аварийно завершило работу.");
}
finally
{
    Log.CloseAndFlush();
}