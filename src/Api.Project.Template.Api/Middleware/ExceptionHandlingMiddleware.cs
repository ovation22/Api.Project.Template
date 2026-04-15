using Api.Project.Template.Application.Abstractions.Logging;

namespace Api.Project.Template.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILoggerAdapter<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Request was canceled by the client. Path {path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request. Path {path}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                logger.LogWarning("The response has already started, cannot write problem details. Path {path}", context.Request.Path);
                throw;
            }

            var problem = Results.Problem(
                title: "An unexpected error occurred.",
                detail: "Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError
            );

            await problem.ExecuteAsync(context);
        }
    }
}
