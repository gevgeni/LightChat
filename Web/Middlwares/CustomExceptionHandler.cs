using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Diagnostics;

namespace LightChat.Web.Middlwares;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Произошло исключение: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Title = "Доступ запрещён";
                problemDetails.Detail = exception.Message;
                break;

            case KeyNotFoundException:
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Не найдено";
                problemDetails.Detail = exception.Message;
                break;

            case InvalidOperationException:
            case ArgumentException:
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Некорректный запрос";
                problemDetails.Detail = exception.Message;
                break;

            case HubException:
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Title = "Ошибка SignalR";
                problemDetails.Detail = exception.Message;
                break;

            default:
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Внутренняя ошибка сервера";
                problemDetails.Detail = "На сервере произошла непредвиденная ошибка. Мы уже работаем над её устранением.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}