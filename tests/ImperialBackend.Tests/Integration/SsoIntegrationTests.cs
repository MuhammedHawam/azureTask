using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Moq;
using Moq.Protected;

namespace ImperialBackend.Tests.Integration;

/// <summary>
/// Integration tests for SSO flow between 360 Salesforce and Imperial Backend API
/// </summary>
public class SsoIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly RSA _rsaKey;
    private readonly string _tenantId = "12345678-1234-5678-9012-123456789012";
    private readonly string _clientId = "87654321-4321-8765-2109-876543210987";

    public SsoIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _rsaKey = RSA.Create(2048);
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override configuration for testing
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["AzureAd:TenantId"] = _tenantId,
                        ["AzureAd:ClientId"] = _clientId,
                        ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                        ["JWT:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long-for-security",
                        ["JWT:Issuer"] = "ImperialBackend",
                        ["JWT:Audience"] = "ImperialBackend.Api",
                        ["JWT:ExpirationHours"] = "8",
                        ["Authorization:AllowedDomains:0"] = "testcompany.com",
                        ["Authorization:AllowedDomains:1"] = "salesforce.com",
                        ["Security:MaxTokenAgeMinutes"] = "60"
                    })
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);
            });
        });

        _client = _factory.CreateClient();
    }

    #region Mock Azure AD Token Generation

    /// <summary>
    /// Creates a mock Azure AD token that mimics tokens from 360 Salesforce
    /// </summary>
    private string CreateMockAzureAdToken(
        string email = "john.doe@testcompany.com",
        string name = "John Doe",
        string userId = "user123",
        DateTime? expiry = null,
        string tenantId = null,
        bool invalidSignature = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var claims = new List<Claim>
        {
            new("oid", userId),
            new("sub", userId),
            new("email", email),
            new("preferred_username", email),
            new("name", name),
            new("given_name", name.Split(' ')[0]),
            new("family_name", name.Split(' ').Length > 1 ? name.Split(' ')[1] : ""),
            new("tid", tenantId ?? _tenantId),
            new("aud", _clientId),
            new("iss", $"https://login.microsoftonline.com/{tenantId ?? _tenantId}/v2.0"),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("amr", "pwd"), // Authentication method: password
            new("ver", "2.0"),
            new("unique_name", email)
        };

        var key = invalidSignature ? RSA.Create(2048) : _rsaKey;
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(key), 
            SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiry ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = signingCredentials,
            Issuer = $"https://login.microsoftonline.com/{tenantId ?? _tenantId}/v2.0",
            Audience = _clientId
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Creates a mock OpenID Connect configuration for Azure AD
    /// </summary>
    private OpenIdConnectConfiguration CreateMockOpenIdConfiguration()
    {
        var config = new OpenIdConnectConfiguration
        {
            Issuer = $"https://login.microsoftonline.com/{_tenantId}/v2.0",
            AuthorizationEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/authorize",
            TokenEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token",
            JwksUri = $"https://login.microsoftonline.com/{_tenantId}/discovery/v2.0/keys"
        };

        // Add the mock RSA key
        var rsaKey = new RsaSecurityKey(_rsaKey);
        config.SigningKeys.Add(rsaKey);

        return config;
    }

    #endregion

    #region Test Cases

    [Fact]
    public async Task ValidateSsoToken_WithValidAzureAdToken_ShouldReturnSuccess()
    {
        // Arrange
        var mockToken = CreateMockAzureAdToken(
            email: "john.doe@testcompany.com",
            name: "John Doe",
            userId: "user123"
        );

        var request = new
        {
            accessToken = mockToken,
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SsoValidationResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.User);
        Assert.Equal("john.doe@testcompany.com", result.User.Email);
        Assert.Equal("John Doe", result.User.Name);
        Assert.Equal("user123", result.User.Id);
        Assert.NotNull(result.SessionToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateSsoToken_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var expiredToken = CreateMockAzureAdToken(
            email: "john.doe@testcompany.com",
            expiry: DateTime.UtcNow.AddHours(-1) // Expired 1 hour ago
        );

        var request = new
        {
            accessToken = expiredToken,
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidateSsoToken_WithInvalidDomain_ShouldReturnForbidden()
    {
        // Arrange
        var tokenWithInvalidDomain = CreateMockAzureAdToken(
            email: "hacker@malicious.com", // Not in allowed domains
            name: "Malicious User"
        );

        var request = new
        {
            accessToken = tokenWithInvalidDomain,
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidateSsoToken_WithInvalidTenant_ShouldReturnForbidden()
    {
        // Arrange
        var tokenWithWrongTenant = CreateMockAzureAdToken(
            email: "john.doe@testcompany.com",
            tenantId: "wrong-tenant-id-12345678-1234-5678-9012"
        );

        var request = new
        {
            accessToken = tokenWithWrongTenant,
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidateSsoToken_WithEmptyToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            accessToken = "",
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidateSsoToken_WithInvalidTokenFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            accessToken = "invalid.token.format",
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EndToEndFlow_SalesforceToApiAccess_ShouldWork()
    {
        // Arrange - Simulate 360 Salesforce user login
        var salesforceUserToken = CreateMockAzureAdToken(
            email: "salesforce.user@testcompany.com",
            name: "Salesforce User",
            userId: "sf-user-123"
        );

        // Step 1: Validate SSO token (simulating Salesforce calling our API)
        var ssoRequest = new
        {
            accessToken = salesforceUserToken,
            source = "salesforce-360"
        };

        var ssoJson = JsonSerializer.Serialize(ssoRequest);
        var ssoContent = new StringContent(ssoJson, Encoding.UTF8, "application/json");

        var ssoResponse = await _client.PostAsync("/api/auth/validate-sso", ssoContent);
        var ssoResponseContent = await ssoResponse.Content.ReadAsStringAsync();
        var ssoResult = JsonSerializer.Deserialize<SsoValidationResponse>(ssoResponseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert SSO validation success
        Assert.True(ssoResponse.IsSuccessStatusCode);
        Assert.NotNull(ssoResult);
        Assert.True(ssoResult.IsValid);
        Assert.NotNull(ssoResult.SessionToken);

        // Step 2: Use session token to access protected API (simulating business API calls)
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ssoResult.SessionToken);

        // Step 3: Test API access with session token
        var outletsResponse = await _client.GetAsync("/api/outlets?pageSize=5");
        Assert.True(outletsResponse.IsSuccessStatusCode);

        // Step 4: Test user info endpoint
        var userInfoResponse = await _client.GetAsync("/api/auth/me");
        Assert.True(userInfoResponse.IsSuccessStatusCode);

        var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfo>(userInfoContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(userInfo);
        Assert.Equal("salesforce.user@testcompany.com", userInfo.Email);
        Assert.Equal("Salesforce User", userInfo.Name);
    }

    [Fact]
    public async Task SessionRefresh_WithValidSession_ShouldReturnNewToken()
    {
        // Arrange - Get initial session
        var initialToken = CreateMockAzureAdToken(
            email: "test.user@testcompany.com",
            name: "Test User"
        );

        var ssoRequest = new
        {
            accessToken = initialToken,
            source = "salesforce-360"
        };

        var ssoJson = JsonSerializer.Serialize(ssoRequest);
        var ssoContent = new StringContent(ssoJson, Encoding.UTF8, "application/json");

        var ssoResponse = await _client.PostAsync("/api/auth/validate-sso", ssoContent);
        var ssoResult = JsonSerializer.Deserialize<SsoValidationResponse>(
            await ssoResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Act - Refresh session
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ssoResult.SessionToken);

        var refreshResponse = await _client.PostAsync("/api/auth/refresh-session", 
            new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert
        Assert.True(refreshResponse.IsSuccessStatusCode);
        
        var refreshResult = JsonSerializer.Deserialize<SessionRefreshResponse>(
            await refreshResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(refreshResult);
        Assert.NotNull(refreshResult.SessionToken);
        Assert.NotEqual(ssoResult.SessionToken, refreshResult.SessionToken); // Should be different
        Assert.True(refreshResult.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Logout_WithValidSession_ShouldReturnSuccess()
    {
        // Arrange - Get session
        var token = CreateMockAzureAdToken(email: "test.user@testcompany.com");
        var ssoRequest = new { accessToken = token, source = "salesforce-360" };
        var ssoContent = new StringContent(JsonSerializer.Serialize(ssoRequest), Encoding.UTF8, "application/json");
        var ssoResponse = await _client.PostAsync("/api/auth/validate-sso", ssoContent);
        var ssoResult = JsonSerializer.Deserialize<SsoValidationResponse>(
            await ssoResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Act - Logout
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ssoResult.SessionToken);

        var logoutResponse = await _client.PostAsync("/api/auth/logout", 
            new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert
        Assert.True(logoutResponse.IsSuccessStatusCode);
    }

    #endregion

    #region Security Attack Simulation Tests

    [Fact]
    public async Task SecurityTest_TokenWithInvalidSignature_ShouldFail()
    {
        // Arrange - Create token with different signing key
        var maliciousToken = CreateMockAzureAdToken(
            email: "attacker@testcompany.com",
            invalidSignature: true
        );

        var request = new
        {
            accessToken = maliciousToken,
            source = "salesforce-360"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecurityTest_ReplayAttackWithOldToken_ShouldFail()
    {
        // Arrange - Create old token (issued 2 hours ago)
        var handler = new JwtSecurityTokenHandler();
        var oldClaims = new List<Claim>
        {
            new("oid", "user123"),
            new("email", "user@testcompany.com"),
            new("name", "User"),
            new("tid", _tenantId),
            new("aud", _clientId),
            new("iss", $"https://login.microsoftonline.com/{_tenantId}/v2.0"),
            new("iat", DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds().ToString()) // 2 hours old
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(oldClaims),
            Expires = DateTime.UtcNow.AddMinutes(30), // Still valid but old
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsaKey), SecurityAlgorithms.RsaSha256),
            Issuer = $"https://login.microsoftonline.com/{_tenantId}/v2.0",
            Audience = _clientId
        };

        var oldToken = handler.WriteToken(handler.CreateToken(tokenDescriptor));

        var request = new
        {
            accessToken = oldToken,
            source = "salesforce-360"
        };

        // Act
        var response = await _client.PostAsync("/api/auth/validate-sso", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert - Should fail due to token age check
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task PerformanceTest_MultipleSimultaneousValidations_ShouldHandle()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            var token = CreateMockAzureAdToken(
                email: $"user{i}@testcompany.com",
                name: $"User {i}",
                userId: $"user{i}"
            );

            var request = new { accessToken = token, source = "salesforce-360" };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            
            tasks.Add(_client.PostAsync("/api/auth/validate-sso", content));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
        _rsaKey?.Dispose();
    }
}

#region DTOs for Testing

public class SsoValidationResponse
{
    public bool IsValid { get; set; }
    public UserInfo User { get; set; }
    public string SessionToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string ErrorMessage { get; set; }
}

public class UserInfo
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string TenantId { get; set; }
}

public class SessionRefreshResponse
{
    public string SessionToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

#endregion