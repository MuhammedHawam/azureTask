using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;

namespace ImperialBackend.Api.Controllers;

/// <summary>
/// Handles authentication and SSO token validation for 360 Salesforce integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the AuthController
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="configuration">The configuration</param>
    public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Validates Azure AD token from 360 Salesforce and returns user context
    /// </summary>
    /// <param name="request">Token validation request</param>
    /// <returns>User context and validation result</returns>
    [HttpPost("validate-sso")]
    [AllowAnonymous]
    public async Task<ActionResult<SsoValidationResponse>> ValidateSsoToken([FromBody] SsoValidationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                _logger.LogWarning("SSO validation attempted with empty token");
                return BadRequest(new { error = "Token is required" });
            }

            // Step 1: Validate token structure
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(request.AccessToken))
            {
                _logger.LogWarning("Invalid JWT token format");
                return BadRequest(new { error = "Invalid token format" });
            }

            var jsonToken = tokenHandler.ReadJwtToken(request.AccessToken);

            // Step 2: Validate token against Azure AD
            var validationResult = await ValidateTokenWithAzureAd(request.AccessToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Token validation failed: {Reason}", validationResult.ErrorMessage);
                return Unauthorized(new { error = validationResult.ErrorMessage });
            }

            // Step 3: Extract user information
            var userInfo = ExtractUserInfo(jsonToken);

            // Step 4: Verify user has access to Imperial Backend
            var hasAccess = await VerifyUserAccess(userInfo);
            if (!hasAccess)
            {
                _logger.LogWarning("User {Email} does not have access to Imperial Backend", userInfo.Email);
                return Forbid("User does not have access to this application");
            }

            // Step 5: Generate application-specific context
            var sessionToken = await GenerateSessionToken(userInfo);

            _logger.LogInformation("SSO validation successful for user {Email}", userInfo.Email);

            return Ok(new SsoValidationResponse
            {
                IsValid = true,
                User = userInfo,
                SessionToken = sessionToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8) // 8-hour session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSO token validation");
            return StatusCode(500, new { error = "Internal server error during validation" });
        }
    }

    /// <summary>
    /// Refreshes the session token for continued access
    /// </summary>
    /// <param name="request">Token refresh request</param>
    /// <returns>New session token</returns>
    [HttpPost("refresh-session")]
    [Authorize]
    public async Task<ActionResult<SessionRefreshResponse>> RefreshSession([FromBody] SessionRefreshRequest request)
    {
        try
        {
            var currentUser = ExtractCurrentUser();
            var newSessionToken = await GenerateSessionToken(currentUser);

            _logger.LogInformation("Session refreshed for user {Email}", currentUser.Email);

            return Ok(new SessionRefreshResponse
            {
                SessionToken = newSessionToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session refresh");
            return StatusCode(500, new { error = "Internal server error during refresh" });
        }
    }

    /// <summary>
    /// Validates the current user's session and returns user context
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        try
        {
            var userInfo = ExtractCurrentUser();
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Logs out the user and invalidates the session
    /// </summary>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        try
        {
            var userInfo = ExtractCurrentUser();
            _logger.LogInformation("User {Email} logged out", userInfo.Email);
            
            // Here you could add session invalidation logic if using a session store
            
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "Internal server error during logout" });
        }
    }

    #region Private Methods

    /// <summary>
    /// Validates token against Azure AD using OpenID Connect discovery
    /// </summary>
    private async Task<TokenValidationResult> ValidateTokenWithAzureAd(string token)
    {
        try
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var metadataUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid_configuration";

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataUrl,
                new OpenIdConnectConfigurationRetriever());

            var config = await configManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            return new TokenValidationResult { IsValid = true };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult { IsValid = false, ErrorMessage = "Token has expired" };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult { IsValid = false, ErrorMessage = "Invalid token signature" };
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return new TokenValidationResult { IsValid = false, ErrorMessage = "Invalid token audience" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation error");
            return new TokenValidationResult { IsValid = false, ErrorMessage = "Token validation failed" };
        }
    }

    /// <summary>
    /// Extracts user information from JWT token
    /// </summary>
    private UserInfo ExtractUserInfo(JwtSecurityToken token)
    {
        return new UserInfo
        {
            Id = token.Claims.FirstOrDefault(c => c.Type == "oid" || c.Type == "sub")?.Value ?? "",
            Email = token.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username")?.Value ?? "",
            Name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "",
            GivenName = token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "",
            FamilyName = token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "",
            Roles = token.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList(),
            TenantId = token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value ?? ""
        };
    }

    /// <summary>
    /// Extracts current user from HTTP context
    /// </summary>
    private UserInfo ExtractCurrentUser()
    {
        return new UserInfo
        {
            Id = User.GetObjectId() ?? "",
            Email = User.GetLoginHint() ?? User.Identity?.Name ?? "",
            Name = User.GetDisplayName() ?? "",
            Roles = User.FindAll("roles").Select(c => c.Value).ToList(),
            TenantId = User.GetTenantId() ?? ""
        };
    }

    /// <summary>
    /// Verifies if user has access to Imperial Backend application
    /// </summary>
    private async Task<bool> VerifyUserAccess(UserInfo userInfo)
    {
        // Implement your access control logic here
        // Examples:
        // 1. Check against database of authorized users
        // 2. Verify user roles
        // 3. Check Azure AD group membership
        // 4. Validate domain restrictions

        try
        {
            // Basic validation: ensure user has required information
            if (string.IsNullOrWhiteSpace(userInfo.Email) || string.IsNullOrWhiteSpace(userInfo.Id))
            {
                return false;
            }

            // Example: Check if user belongs to specific domain
            var allowedDomains = _configuration.GetSection("Authorization:AllowedDomains").Get<string[]>() ?? Array.Empty<string>();
            if (allowedDomains.Any())
            {
                var userDomain = userInfo.Email.Split('@').LastOrDefault();
                if (!allowedDomains.Contains(userDomain, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Example: Check required roles
            var requiredRoles = _configuration.GetSection("Authorization:RequiredRoles").Get<string[]>() ?? Array.Empty<string>();
            if (requiredRoles.Any())
            {
                if (!requiredRoles.Any(role => userInfo.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Add your custom authorization logic here
            await Task.CompletedTask; // Placeholder for async operations

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying user access for {Email}", userInfo.Email);
            return false;
        }
    }

    /// <summary>
    /// Generates application-specific session token
    /// </summary>
    private async Task<string> GenerateSessionToken(UserInfo userInfo)
    {
        // This creates a new JWT token specific to your application
        // with limited scope and shorter expiration

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = System.Text.Encoding.ASCII.GetBytes(_configuration["JWT:SecretKey"] ?? "your-secret-key-should-be-in-config");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("user_id", userInfo.Id),
                new Claim("email", userInfo.Email),
                new Claim("name", userInfo.Name),
                new Claim("tenant_id", userInfo.TenantId)
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = "ImperialBackend",
            Audience = "ImperialBackend.Api"
        };

        // Add roles as claims
        foreach (var role in userInfo.Roles)
        {
            ((ClaimsIdentity)tokenDescriptor.Subject).AddClaim(new Claim("role", role));
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        await Task.CompletedTask; // Placeholder for any async operations

        return tokenString;
    }

    #endregion
}

#region DTOs

/// <summary>
/// Request model for SSO token validation
/// </summary>
public record SsoValidationRequest
{
    /// <summary>
    /// The Azure AD access token from 360 Salesforce
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Optional: The application context or source
    /// </summary>
    public string? Source { get; init; }
}

/// <summary>
/// Response model for SSO token validation
/// </summary>
public record SsoValidationResponse
{
    /// <summary>
    /// Whether the token validation was successful
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// User information extracted from the token
    /// </summary>
    public UserInfo? User { get; init; }

    /// <summary>
    /// Application-specific session token
    /// </summary>
    public string? SessionToken { get; init; }

    /// <summary>
    /// When the session token expires
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// User information model
/// </summary>
public record UserInfo
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string GivenName { get; init; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string FamilyName { get; init; } = string.Empty;

    /// <summary>
    /// User's roles in the application
    /// </summary>
    public List<string> Roles { get; init; } = new();

    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
}

/// <summary>
/// Request model for session refresh
/// </summary>
public record SessionRefreshRequest
{
    /// <summary>
    /// Current session token to refresh
    /// </summary>
    public string SessionToken { get; init; } = string.Empty;
}

/// <summary>
/// Response model for session refresh
/// </summary>
public record SessionRefreshResponse
{
    /// <summary>
    /// New session token
    /// </summary>
    public string SessionToken { get; init; } = string.Empty;

    /// <summary>
    /// When the new session token expires
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Internal model for token validation results
/// </summary>
internal record TokenValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
}

#endregion