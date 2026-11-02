using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cart.Infrastructure.Outbox;
using Serilog.Context;

namespace Cart.Api.Correlation;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreate(context.Request);

        Activity.Current?.SetBaggage(OutboxMessage.CorrelationIdBaggage, correlationId);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string GetOrCreate(HttpRequest request)
    {
        string? value = request.Headers[HeaderName];
        return IsValid(value) ? value : Guid.NewGuid().ToString();
    }

    private static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
