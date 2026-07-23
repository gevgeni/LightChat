using System.Security.Claims;

using Serilog;
using MediatR;
using FluentValidation;
using StackExchange.Redis;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using LightChat.Web.Hubs;
using LightChat.Web.Requests;
using LightChat.Web.Services;
using LightChat.Web.Extensions;
using LightChat.Web.Middlwares;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Security;
using LightChat.Infrastructure.Persistence;
using LightChat.Infrastructure.Repositories;

using LightChat.Core.Features.Messages.GetMessageHistory;
using LightChat.Core.Features.Users.UserJwtAuthorize;
using LightChat.Core.Features.Users.UserRegister;
using LightChat.Core.Features.Users.GetAllUsers;
using LightChat.Core.Features.Chats.GetChatMembers;
using LightChat.Core.Features.Chats.AddChatMember;
using LightChat.Core.Features.Chats.GetUserChats;
using LightChat.Core.Features.Chats.CreateChat;

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

    builder.Services.AddExceptionHandler<CustomExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LightChat.Core.AssemblyMarker).Assembly));
    builder.Services.AddValidatorsFromAssembly(typeof(LightChat.Core.AssemblyMarker).Assembly);

    #region Настройка Redis
    var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
    #endregion

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

    builder.Services.AddSingleton<IUserStatusManager, UserStatusManager>();
    builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

    #region JWT авторизация
    builder.Services.AddWebAuthentication(builder.Configuration);
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
    app.MapPost("/api/users", async (CreateUserRequest dto, ISender mediatr, IValidator<UserRegisterCommand> validator) =>
    {
        try
        {
            var command = new UserRegisterCommand(
                dto.Username,
                dto.Email,
                dto.Password);

            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var result = await mediatr.Send(command);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
    });

    //endpoint - авторизация пользователя с Json Web Token
    app.MapPost("/auth/login", async (LoginRequest request, IValidator<UserJwtAuthorizeQuery> validator, ISender mediatr) =>
    {
        try
        {
            var command = new UserJwtAuthorizeQuery(request.Username, request.Password);

            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var result = await mediatr.Send(command);

            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    });

    //endpoint - получение истории сообщений
    app.MapGet("chats/{chatId:guid}/messages", async (
        Guid chatId,
        int limit,
        Guid? beforeMessageId,
        ClaimsPrincipal user, 
        ISender mediatr) =>
    {
        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            return Results.Unauthorized();

        try
        {
            var command = new GetMessageHistoryQuery(chatId, userId, limit, beforeMessageId);
            var result = await mediatr.Send(command);

            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization();

    //endpoint - получение всех чатов пользователя
    app.MapGet("/chats", async (ClaimsPrincipal user, ISender mediatr) =>
    {
        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            return Results.Unauthorized();

        var query = new GetUserChatsQuery(userId);
        var result = await mediatr.Send(query);

        return Results.Ok(result);
    })
    .RequireAuthorization();

    //endpoint - создание групового чата
    app.MapPost("/chats", async (CreateChatRequest request, IValidator<CreateChatCommand> validator, ClaimsPrincipal principal, ISender mediatr) =>
    {
        var nameIdentifier = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            return Results.Unauthorized();

        var command = new CreateChatCommand(request.Name, userId);

        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var result = await mediatr.Send(command);

        return Results.Created($"/chats/{result.Id}", result);
    })
    .RequireAuthorization();

    //endpoint - создание личного чата
    app.MapPost("/chats/direct", async (CreateDirectChatRequest request, ClaimsPrincipal principal, ISender mediatr) =>
    {
        var currentUserIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(currentUserIdString, out var currentUserId)) return Results.Unauthorized();

        var targetUserId = request.TargetUserId;
        if (targetUserId == currentUserId) return Results.BadRequest("Нельзя создать личный чат с самим собой.");

        var command = new CreateDirectChatCommand(currentUserId, request.TargetUserId);
        var result = await mediatr.Send(command);

        return Results.Created($"/chats/direct/{result.Id}", result);
    })
    .RequireAuthorization();

    //endpoint - получение участников чата
    app.MapGet("/chats/{chatId}/members", async (Guid chatId, ISender mediatr) =>
    {
        var query = new GetChatMembersQuery(chatId);
        var result = await mediatr.Send(query);

        return Results.Ok(result);
    })
    .RequireAuthorization();

    //endpoint - добавление участников в чат
    app.MapPost("/chats/{chatId:guid}/members", async (
        Guid chatId,
        ISender mediatr,
        ClaimsPrincipal user,
        AddMemberRequest request,
        IHubContext <ChatHub> hubContext) =>
    {
        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            return Results.Unauthorized();

        try
        {
            var command = new AddChatMemberCommand(chatId, request.UserId, userId);
            var result = await mediatr.Send(command);

            await hubContext.Clients.User(request.UserId.ToString()).SendAsync("ChatInvitation", new
            {
                id = result.ChatId,
                name = result.ChatName,
                isDirect = result.IsDirect
            });

            return Results.Ok("Пользователь успешно добавлен в чат.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("состоит в этом чате"))
                return Results.Conflict(ex.Message);

            return Results.BadRequest(ex.Message);
        }
    })
    .RequireAuthorization();

    //endpoint - получение всех пользователей
    app.MapGet("/users", async (ClaimsPrincipal user, ISender mediatr) =>
    {
        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var currentUserId))
            return Results.Unauthorized();

        var query = new GetAllUsersQuery(currentUserId);
        var result = await mediatr.Send(query);

        return Results.Ok(result);
    })
    .RequireAuthorization();
    #endregion

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<ChatHub>("/chatHub");

    using (var scope = app.Services.CreateScope())
    {
        var statusManager = scope.ServiceProvider.GetRequiredService<IUserStatusManager>();
        statusManager.ClearAllStatuses();
        Log.Information("Статусы пользователей в Redis успешно очищены при запуске.");
    }

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