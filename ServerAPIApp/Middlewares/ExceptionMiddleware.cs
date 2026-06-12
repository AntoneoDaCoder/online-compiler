using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using ServerAPIApp.Domain.Exceptions.ConflictExceptions;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions;
using System.Text.Json;

namespace ServerAPIApp.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true };

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (BadRequestException ex)
            {
                await WriteResponseAsync(httpContext, 400, ex.Message, ex);
            }
            catch (UnauthorizedException ex)
            {
                await WriteResponseAsync(httpContext, 401, ex.Message, ex);
            }
            catch (ForbiddenException ex)
            {
                await WriteResponseAsync(httpContext, 403, ex.Message, ex);
            }
            catch (ResourceNotFoundException ex)
            {
                await WriteResponseAsync(httpContext, 404, ex.Message, ex);
            }
            catch (ConflictException ex)
            {
                await WriteResponseAsync(httpContext, 409, ex.Message, ex);
            }
            catch (InternalServerException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(ex, "Server exception occured: {Message}", ex.Message);

                await WriteResponseAsync(httpContext, 500, ex.Message, ex);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(ex, "Unhandled exception occured: {Message}", ex.Message);

                await WriteResponseAsync(httpContext, 500, ex.Message, ex);
            }
        }

        private static List<string> GetErrorMessages(Exception ex)
        {
            var messages = new List<string>();

            while (ex != null)
            {
                messages.Add(ex.Message);

                ex = ex.InnerException;
            }

            return messages;
        }

        private static async Task WriteResponseAsync(HttpContext httpContext, int statusCode, string message, Exception exception)
        {
            httpContext.Response.ContentType = "application/json";

            httpContext.Response.StatusCode = statusCode;

            var errorResponse = new
            {
                Message = message,
                Details = GetErrorMessages(exception),
                OccuredAt = exception.StackTrace
            };

            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _options));
        }
    }
}
