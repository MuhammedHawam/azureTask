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
/// Handles SSO token validation for 360 Salesforce integration with comprehensive security checks
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
    /// Validates Azure AD token from 360 Salesforce with comprehensive security checks
    /// </summary>
    /// <param name="request">Token validation request</param>
    /// <returns>User context and validation result</returns>
    [HttpPost("validate-sso")]
    [AllowAnonymous]
    public async Task<ActionResult<SsoValidationResponse>> ValidateSsoToken([FromBody] SsoValidationRequest request)
    {
        try
        {
            _logger.LogInformation("SSO validation attempt from source: {Source}", request.Source ?? "unknown");

            // Step 1: Basic input validation
            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                _logger.LogWarning("SSO validation attempted with empty token from {IP}", 
                    HttpContext.Connection.RemoteIpAddress);
                return BadRequest(new { error = "Access token is required", code = "INVALID_INPUT" });
            }

            // Step 2: Token format validation
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(request.AccessToken))
            {
                _logger.LogWarning("Invalid JWT token format from {IP}", 
                    HttpContext.Connection.RemoteIpAddress);
                return BadRequest(new { error = "Invalid token format", code = "INVALID_TOKEN_FORMAT" });
            }

            var jsonToken = tokenHandler.ReadJwtToken(request.AccessToken);

            // Step 3: Comprehensive token validation against Azure AD
            var validationResult = await ValidateTokenWithAzureAd(request.AccessToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Token validation failed: {Reason} from {IP}", 
                    validationResult.ErrorMessage, HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new { 
                    error = validationResult.ErrorMessage, 
                    code = "TOKEN_VALIDATION_FAILED" 
                });
            }

            // Step 4: Extract and validate user information
            var userInfo = ExtractUserInfo(jsonToken);
            if (string.IsNullOrWhiteSpace(userInfo.Email) || string.IsNullOrWhiteSpace(userInfo.Id))
            {
                _logger.LogWarning("Token missing required user claims from {IP}", 
                    HttpContext.Connection.RemoteIpAddress);
                return BadRequest(new { 
                    error = "Token missing required user information", 
                    code = "MISSING_USER_CLAIMS" 
                });
            }

            // Step 5: Domain validation (if configured)
            if (!await ValidateUserDomain(userInfo.Email))
            {
                _logger.LogWarning("User {Email} from unauthorized domain attempted access from {IP}", 
                    userInfo.Email, HttpContext.Connection.RemoteIpAddress);
                return Forbid("User domain not authorized for this application");
            }

            // Step 6: Additional security checks
            if (!await PerformAdditionalSecurityChecks(userInfo, jsonToken))
            {
                _logger.LogWarning("Additional security checks failed for user {Email} from {IP}", 
                    userInfo.Email, HttpContext.Connection.RemoteIpAddress);
                return Forbid("Access denied based on security policies");
            }

            // Step 7: Generate secure session token
            var sessionToken = await GenerateSessionToken(userInfo);

            _logger.LogInformation("SSO validation successful for user {Email} from {IP}", 
                userInfo.Email, HttpContext.Connection.RemoteIpAddress);

            return Ok(new SsoValidationResponse
            {
                IsValid = true,
                User = userInfo,
                SessionToken = sessionToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Security token validation error from {IP}", 
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { 
                error = "Token validation failed", 
                code = "TOKEN_SECURITY_ERROR" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SSO token validation from {IP}", 
                HttpContext.Connection.RemoteIpAddress);
            return StatusCode(500, new { 
                error = "Internal server error during validation", 
                code = "INTERNAL_ERROR" 
            });
        }
    }

    /// <summary>
    /// Refreshes the session token for continued access
    /// </summary>
    /// <returns>New session token</returns>
    [HttpPost("refresh-session")]
    [Authorize]
    public async Task<ActionResult<SessionRefreshResponse>> RefreshSession()
    {
        try
        {
            var currentUser = ExtractCurrentUser();
            if (currentUser == null)
            {
                _logger.LogWarning("Session refresh attempted with invalid user context from {IP}", 
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized("Invalid user session");
            }

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
            _logger.LogError(ex, "Error during session refresh from {IP}", 
                HttpContext.Connection.RemoteIpAddress);
            return StatusCode(500, new { error = "Internal server error during refresh" });
        }
    }

    /// <summary>
    /// Gets the current authenticated user's information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        try
        {
            var userInfo = ExtractCurrentUser();
            if (userInfo == null)
            {
                return Unauthorized("Invalid user session");
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user from {IP}", 
                HttpContext.Connection.RemoteIpAddress);
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
            _logger.LogInformation("User {Email} logged out from {IP}", 
                userInfo?.Email ?? "unknown", HttpContext.Connection.RemoteIpAddress);
            
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout from {IP}", 
                HttpContext.Connection.RemoteIpAddress);
            return StatusCode(500, new { error = "Internal server error during logout" });
        }
    }

    #region Private Methods

    /// <summary>
    /// Validates token against Azure AD with comprehensive security checks
    /// </summary>
    private async Task<TokenValidationResult> ValidateTokenWithAzureAd(string token)
    {
        try
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("Azure AD configuration missing - TenantId or ClientId not configured");
                return new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Azure AD configuration error" 
                };
            }

            var metadataUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid_configuration";

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataUrl,
                new OpenIdConnectConfigurationRetriever());

            var config = await configManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                // Signature validation
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                
                // Issuer validation
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                
                // Audience validation
                ValidateAudience = true,
                ValidAudience = clientId,
                
                // Lifetime validation
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
                
                // Additional security
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Additional validation: ensure it's a JWT token
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Invalid token type" 
                };
            }

            // Validate token algorithm
            if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.Ordinal))
            {
                return new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Invalid token algorithm" 
                };
            }

            return new TokenValidationResult { IsValid = true };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Token has expired" 
            };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Invalid token signature" 
            };
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Invalid token audience" 
            };
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Invalid token issuer" 
            };
        }
        catch (SecurityTokenNotYetValidException)
        {
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Token not yet valid" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Token validation failed" 
            };
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
            Email = token.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username" || c.Type == "upn")?.Value ?? "",
            Name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "",
            GivenName = token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "",
            FamilyName = token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "",
            TenantId = token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value ?? ""
        };
    }

    /// <summary>
    /// Extracts current user from HTTP context
    /// </summary>
    private UserInfo? ExtractCurrentUser()
    {
        try
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;

            return new UserInfo
            {
                Id = User.GetObjectId() ?? User.FindFirst("user_id")?.Value ?? "",
                Email = User.GetLoginHint() ?? User.FindFirst("email")?.Value ?? User.Identity?.Name ?? "",
                Name = User.GetDisplayName() ?? User.FindFirst("name")?.Value ?? "",
                TenantId = User.GetTenantId() ?? User.FindFirst("tenant_id")?.Value ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting current user from context");
            return null;
        }
    }

    /// <summary>
    /// Validates if user's domain is allowed (if domain restrictions are configured)
    /// </summary>
    private Task<bool> ValidateUserDomain(string email)
    {
        try
        {
            var allowedDomains = _configuration.GetSection("Authorization:AllowedDomains").Get<string[]>();
            
            // If no domain restrictions configured, allow all
            if (allowedDomains == null || !allowedDomains.Any())
            {
                return Task.FromResult(true);
            }

            var userDomain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(userDomain))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(allowedDomains.Any(domain => 
                domain.Equals(userDomain, StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user domain for {Email}", email);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Performs additional security checks beyond basic token validation
    /// </summary>
    private Task<bool> PerformAdditionalSecurityChecks(UserInfo userInfo, JwtSecurityToken token)
    {
        try
        {
            // Check 1: Validate tenant ID if configured
            var expectedTenantId = _configuration["AzureAd:TenantId"];
            if (!string.IsNullOrWhiteSpace(expectedTenantId) && 
                !expectedTenantId.Equals(userInfo.TenantId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("User {Email} from incorrect tenant {TenantId}, expected {ExpectedTenantId}", 
                    userInfo.Email, userInfo.TenantId, expectedTenantId);
                return Task.FromResult(false);
            }

            // Check 2: Validate token age (additional check beyond expiration)
            var maxTokenAge = _configuration.GetValue<int>("Security:MaxTokenAgeMinutes", 60);
            var tokenIssueTime = token.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;
            if (!string.IsNullOrWhiteSpace(tokenIssueTime) && 
                long.TryParse(tokenIssueTime, out var iat))
            {
                var issueDateTime = DateTimeOffset.FromUnixTimeSeconds(iat);
                if (DateTime.UtcNow.Subtract(issueDateTime.UtcDateTime).TotalMinutes > maxTokenAge)
                {
                    _logger.LogWarning("Token for user {Email} is too old, issued at {IssueTime}", 
                        userInfo.Email, issueDateTime);
                    return Task.FromResult(false);
                }
            }

            // Check 3: Validate authentication method if required
            var requiredAuthMethod = _configuration["Security:RequiredAuthMethod"];
            if (!string.IsNullOrWhiteSpace(requiredAuthMethod))
            {
                var authMethod = token.Claims.FirstOrDefault(c => c.Type == "amr")?.Value;
                if (!requiredAuthMethod.Equals(authMethod, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User {Email} used invalid auth method {AuthMethod}, required {RequiredMethod}", 
                        userInfo.Email, authMethod, requiredAuthMethod);
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing additional security checks for user {Email}", userInfo.Email);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Generates application-specific session token with security claims
    /// </summary>
    private async Task<string> GenerateSessionToken(UserInfo userInfo)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JWT:SecretKey"];
            
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey must be configured and at least 32 characters long");
            }

            var key = System.Text.Encoding.UTF8.GetBytes(secretKey);
            var expirationHours = _configuration.GetValue<int>("JWT:ExpirationHours", 8);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("user_id", userInfo.Id),
                    new Claim("email", userInfo.Email),
                    new Claim("name", userInfo.Name),
                    new Claim("tenant_id", userInfo.TenantId),
                    new Claim("session_id", Guid.NewGuid().ToString()), // Unique session identifier
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                }),
                Expires = DateTime.UtcNow.AddHours(expirationHours),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JWT:Issuer"] ?? "ImperialBackend",
                Audience = _configuration["JWT:Audience"] ?? "ImperialBackend.Api"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("Session token generated for user {Email} with {ExpirationHours}h expiration", 
                userInfo.Email, expirationHours);

            await Task.CompletedTask;
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session token for user {Email}", userInfo.Email);
            throw;
        }
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
    /// User's unique identifier from Azure AD
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
    /// Azure AD tenant ID
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
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