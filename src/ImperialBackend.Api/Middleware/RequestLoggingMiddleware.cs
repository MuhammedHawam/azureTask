using System.Diagnostics;
using System.Text;

namespace ImperialBackend.Api.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the RequestLoggingMiddleware class
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        // Add request ID to the context for correlation
        context.Items["RequestId"] = requestId;
        
        // Log request
        await LogRequestAsync(context, requestId);

        // Capture response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred for request {RequestId}", requestId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        try
        {
            var request = context.Request;
            var requestBody = string.Empty;

            // Read request body if present and not a file upload
            if (request.ContentLength > 0 && 
                request.ContentType != null && 
                !request.ContentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                requestBody = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0;
            }

            var logData = new
            {
                RequestId = requestId,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray())),
                Body = requestBody,
                UserAgent = request.Headers.UserAgent.ToString(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                User = context.User?.Identity?.Name ?? "Anonymous"
            };

            _logger.LogInformation("HTTP Request {RequestId}: {@RequestData}", requestId, logData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request {RequestId}", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMilliseconds)
    {
        try
        {
            var response = context.Response;
            var responseBody = string.Empty;

            // Read response body if it's not too large and is text-based
            if (response.Body.CanSeek && 
                response.ContentLength < 10000 && 
                IsTextBasedContentType(response.ContentType))
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
                responseBody = await reader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
            }

            var logData = new
            {
                RequestId = requestId,
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray())),
                Body = responseBody,
                ElapsedMilliseconds = elapsedMilliseconds
            };

            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(logLevel, "HTTP Response {RequestId}: {@ResponseData}", requestId, logData);

            // Log performance warning for slow requests
            if (elapsedMilliseconds > 5000)
            {
                _logger.LogWarning("Slow request detected {RequestId}: {ElapsedMilliseconds}ms", requestId, elapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log response {RequestId}", requestId);
        }
    }

    private static bool IsTextBasedContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Contains("text/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("application/xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }
}