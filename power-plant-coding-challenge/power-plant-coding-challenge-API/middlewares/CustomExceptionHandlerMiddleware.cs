using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace power_plant_coding_challenge_API.middlewares;

public class CustomExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public CustomExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await ConvertException(context, ex);
        }
    }

    public Task ConvertException(HttpContext context, Exception exception)
    {
        HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

        context.Response.ContentType = "application/json";

        ProblemDetails? responseBody = null;

        switch (exception)
        {
            case ValidationException ex:
                httpStatusCode = HttpStatusCode.BadRequest;
                responseBody = new()
                {
                    Status = (int)httpStatusCode,
                    Title = "Validation Error",
                    Detail = ex.Message
                };
                break;
            case Exception ex:
                httpStatusCode = HttpStatusCode.InternalServerError;
                responseBody = new()
                {
                    Status = (int)httpStatusCode,
                    Title = "Internal Server Error",
                    Detail = ex.Message
                };
                break;
        }

        context.Response.StatusCode = (int)httpStatusCode;

        if (responseBody is null)
        {
            return context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }

        return context.Response.WriteAsJsonAsync(responseBody);
    }
}
