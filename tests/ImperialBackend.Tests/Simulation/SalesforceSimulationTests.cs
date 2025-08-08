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

namespace ImperialBackend.Tests.Simulation;

/// <summary>
/// Simulates the complete 360 Salesforce integration flow with Imperial Backend API
/// This test demonstrates how a real Salesforce app would interact with our SSO endpoints
/// </summary>
public class SalesforceSimulationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _apiClient;
    private readonly RSA _rsaKey;
    private readonly string _tenantId = "salesforce-tenant-12345678-1234-5678-9012";
    private readonly string _clientId = "salesforce-client-87654321-4321-8765-2109";

    public SalesforceSimulationTests(WebApplicationFactory<Program> factory)
    {
        _rsaKey = RSA.Create(2048);
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["AzureAd:TenantId"] = _tenantId,
                        ["AzureAd:ClientId"] = _clientId,
                        ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                        ["JWT:SecretKey"] = "salesforce-test-secret-key-that-is-at-least-32-characters-long",
                        ["JWT:Issuer"] = "ImperialBackend",
                        ["JWT:Audience"] = "ImperialBackend.Api",
                        ["JWT:ExpirationHours"] = "8",
                        ["Authorization:AllowedDomains:0"] = "salesforce.com",
                        ["Authorization:AllowedDomains:1"] = "acme-corp.com",
                        ["Authorization:AllowedDomains:2"] = "testcompany.com",
                        ["Security:MaxTokenAgeMinutes"] = "60",
                        ["SalesforceIntegration:AllowedOrigins:0"] = "https://acme-corp.lightning.force.com",
                        ["SalesforceIntegration:TrustedAppIds:0"] = "salesforce-360-app"
                    })
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);
            });
        });

        _apiClient = _factory.CreateClient();
    }

    /// <summary>
    /// Simulates the complete 360 Salesforce user journey
    /// </summary>
    [Fact]
    public async Task Simulate_Complete360SalesforceUserJourney_ShouldWork()
    {
        // 🎭 SCENE 1: User logs into 360 Salesforce with Azure AD
        var salesforceUser = new SalesforceUser
        {
            Id = "005xx000004TmiAAAS", // Salesforce User ID format
            Email = "john.smith@acme-corp.com",
            Name = "John Smith",
            Department = "Sales",
            Role = "Sales Manager",
            CompanyName = "Acme Corporation"
        };

        // Simulate Azure AD issuing token to Salesforce
        var azureAdToken = CreateMockSalesforceAzureAdToken(salesforceUser);
        
        // 🎭 SCENE 2: User clicks on Imperial Backend app within 360 Salesforce
        // Salesforce frontend calls our SSO validation endpoint
        var ssoValidationRequest = new SalesforceToImperialRequest
        {
            AccessToken = azureAdToken,
            Source = "salesforce-360",
            SalesforceUserId = salesforceUser.Id,
            SalesforceOrgId = "00D000000000062EAA", // Salesforce Org ID format
            RequestId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow
        };

        var ssoResponse = await CallImperialBackendSsoValidation(ssoValidationRequest);

        // Assert SSO validation success
        Assert.True(ssoResponse.IsValid);
        Assert.NotNull(ssoResponse.SessionToken);
        Assert.Equal("john.smith@acme-corp.com", ssoResponse.User.Email);
        
        Console.WriteLine($"✅ SSO Validation Success for {ssoResponse.User.Name}");
        Console.WriteLine($"📧 Email: {ssoResponse.User.Email}");
        Console.WriteLine($"🎫 Session Token: {ssoResponse.SessionToken[..20]}...");

        // 🎭 SCENE 3: Salesforce app uses session token to access Imperial Backend APIs
        var salesforceApiClient = CreateSalesforceAppApiClient(ssoResponse.SessionToken);

        // 3.1: Get user profile information
        var userProfile = await GetUserProfile(salesforceApiClient);
        Assert.NotNull(userProfile);
        Assert.Equal("john.smith@acme-corp.com", userProfile.Email);
        
        Console.WriteLine($"👤 User Profile Retrieved: {userProfile.Name}");

        // 3.2: Fetch outlet data for Salesforce dashboard
        var outletData = await GetOutletDataForSalesforce(salesforceApiClient);
        Assert.NotNull(outletData);
        
        Console.WriteLine($"🏪 Retrieved {outletData.Count} outlets for Salesforce dashboard");

        // 3.3: Get specific outlet by ID (as called from Salesforce record page)
        if (outletData.Count > 0)
        {
            var firstOutlet = outletData[0];
            var outletDetails = await GetOutletById(salesforceApiClient, firstOutlet.Id);
            Assert.NotNull(outletDetails);
            Assert.Equal(firstOutlet.Id, outletDetails.Id);
            
            Console.WriteLine($"🔍 Retrieved outlet details: {outletDetails.Name}");
        }

        // 🎭 SCENE 4: Session refresh (simulating long-running Salesforce session)
        await Task.Delay(100); // Simulate some time passing
        
        var refreshedSession = await RefreshSession(salesforceApiClient);
        Assert.NotNull(refreshedSession.SessionToken);
        Assert.NotEqual(ssoResponse.SessionToken, refreshedSession.SessionToken);
        
        Console.WriteLine($"🔄 Session refreshed successfully");

        // 🎭 SCENE 5: User logs out from Salesforce (or session expires)
        var logoutSuccess = await LogoutFromImperialBackend(salesforceApiClient);
        Assert.True(logoutSuccess);
        
        Console.WriteLine($"👋 User logged out successfully");
    }

    /// <summary>
    /// Simulates multiple Salesforce users accessing the system simultaneously
    /// </summary>
    [Fact]
    public async Task Simulate_MultipleSalesforceUsers_ShouldHandleConcurrency()
    {
        var salesforceUsers = new[]
        {
            new SalesforceUser { Id = "005xx000004TmiAAAS", Email = "alice.johnson@acme-corp.com", Name = "Alice Johnson", Role = "Sales Rep" },
            new SalesforceUser { Id = "005xx000004TmiBBAS", Email = "bob.wilson@acme-corp.com", Name = "Bob Wilson", Role = "Sales Manager" },
            new SalesforceUser { Id = "005xx000004TmiCCAS", Email = "carol.brown@acme-corp.com", Name = "Carol Brown", Role = "VP Sales" },
            new SalesforceUser { Id = "005xx000004TmiDDAS", Email = "david.lee@acme-corp.com", Name = "David Lee", Role = "Account Executive" },
            new SalesforceUser { Id = "005xx000004TmiEEAS", Email = "emma.davis@acme-corp.com", Name = "Emma Davis", Role = "Inside Sales" }
        };

        var tasks = salesforceUsers.Select(async user =>
        {
            // Each user gets Azure AD token and calls SSO validation
            var azureToken = CreateMockSalesforceAzureAdToken(user);
            var ssoRequest = new SalesforceToImperialRequest
            {
                AccessToken = azureToken,
                Source = "salesforce-360",
                SalesforceUserId = user.Id,
                SalesforceOrgId = "00D000000000062EAA",
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            var ssoResponse = await CallImperialBackendSsoValidation(ssoRequest);
            
            // Each user accesses outlet data
            var apiClient = CreateSalesforceAppApiClient(ssoResponse.SessionToken);
            var outlets = await GetOutletDataForSalesforce(apiClient);
            
            return new { User = user, SessionValid = ssoResponse.IsValid, OutletCount = outlets?.Count ?? 0 };
        });

        var results = await Task.WhenAll(tasks);

        // Assert all users successfully authenticated and accessed data
        Assert.All(results, result => 
        {
            Assert.True(result.SessionValid);
            Assert.True(result.OutletCount >= 0);
        });

        Console.WriteLine($"✅ {results.Length} Salesforce users successfully processed concurrently");
        foreach (var result in results)
        {
            Console.WriteLine($"👤 {result.User.Name} ({result.User.Role}): {result.OutletCount} outlets");
        }
    }

    /// <summary>
    /// Simulates error scenarios that Salesforce might encounter
    /// </summary>
    [Fact]
    public async Task Simulate_SalesforceErrorScenarios_ShouldHandleGracefully()
    {
        // Scenario 1: User from unauthorized domain
        var unauthorizedUser = new SalesforceUser
        {
            Id = "005xx000004TmiXXAS",
            Email = "hacker@malicious.com", // Not in allowed domains
            Name = "Malicious User"
        };

        var maliciousToken = CreateMockSalesforceAzureAdToken(unauthorizedUser);
        var maliciousRequest = new SalesforceToImperialRequest
        {
            AccessToken = maliciousToken,
            Source = "salesforce-360",
            SalesforceUserId = unauthorizedUser.Id
        };

        var unauthorizedResponse = await CallImperialBackendSsoValidation(maliciousRequest);
        Assert.False(unauthorizedResponse.IsValid);
        Assert.Contains("domain", unauthorizedResponse.ErrorMessage?.ToLower() ?? "");
        
        Console.WriteLine($"🚫 Unauthorized domain correctly rejected: {unauthorizedUser.Email}");

        // Scenario 2: Expired token
        var validUser = new SalesforceUser
        {
            Id = "005xx000004TmiVVAS",
            Email = "valid.user@acme-corp.com",
            Name = "Valid User"
        };

        var expiredToken = CreateMockSalesforceAzureAdToken(validUser, expiry: DateTime.UtcNow.AddHours(-1));
        var expiredRequest = new SalesforceToImperialRequest
        {
            AccessToken = expiredToken,
            Source = "salesforce-360",
            SalesforceUserId = validUser.Id
        };

        var expiredResponse = await CallImperialBackendSsoValidation(expiredRequest);
        Assert.False(expiredResponse.IsValid);
        Assert.Contains("expired", expiredResponse.ErrorMessage?.ToLower() ?? "");
        
        Console.WriteLine($"⏰ Expired token correctly rejected for: {validUser.Email}");

        // Scenario 3: Invalid token format
        var invalidRequest = new SalesforceToImperialRequest
        {
            AccessToken = "invalid.token.format",
            Source = "salesforce-360",
            SalesforceUserId = validUser.Id
        };

        var invalidResponse = await CallImperialBackendSsoValidation(invalidRequest);
        Assert.False(invalidResponse.IsValid);
        Assert.Contains("format", invalidResponse.ErrorMessage?.ToLower() ?? "");
        
        Console.WriteLine($"🔧 Invalid token format correctly rejected");
    }

    #region Helper Methods

    private string CreateMockSalesforceAzureAdToken(
        SalesforceUser user, 
        DateTime? expiry = null,
        bool invalidSignature = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var claims = new List<Claim>
        {
            new("oid", $"azure-{user.Id}"),
            new("sub", $"azure-{user.Id}"),
            new("email", user.Email),
            new("preferred_username", user.Email),
            new("name", user.Name),
            new("given_name", user.Name.Split(' ')[0]),
            new("family_name", user.Name.Split(' ').Length > 1 ? user.Name.Split(' ')[1] : ""),
            new("tid", _tenantId),
            new("aud", _clientId),
            new("iss", $"https://login.microsoftonline.com/{_tenantId}/v2.0"),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("amr", "pwd"),
            new("ver", "2.0"),
            new("unique_name", user.Email),
            // Salesforce-specific claims
            new("custom:salesforce_user_id", user.Id),
            new("custom:salesforce_role", user.Role ?? "User"),
            new("custom:department", user.Department ?? "Unknown"),
            new("custom:company", user.CompanyName ?? "Unknown")
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
            Issuer = $"https://login.microsoftonline.com/{_tenantId}/v2.0",
            Audience = _clientId
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<SsoValidationResponse> CallImperialBackendSsoValidation(SalesforceToImperialRequest request)
    {
        var json = JsonSerializer.Serialize(new
        {
            accessToken = request.AccessToken,
            source = request.Source
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _apiClient.PostAsync("/api/auth/validate-sso", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<SsoValidationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new SsoValidationResponse { IsValid = false, ErrorMessage = "Deserialization failed" };
            }
            else
            {
                // Parse error response
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var errorMessage = errorObj.TryGetProperty("error", out var errorProp) 
                        ? errorProp.GetString() 
                        : $"HTTP {response.StatusCode}";
                    
                    return new SsoValidationResponse 
                    { 
                        IsValid = false, 
                        ErrorMessage = errorMessage 
                    };
                }
                catch
                {
                    return new SsoValidationResponse 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}" 
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new SsoValidationResponse 
            { 
                IsValid = false, 
                ErrorMessage = $"Request failed: {ex.Message}" 
            };
        }
    }

    private HttpClient CreateSalesforceAppApiClient(string sessionToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);
        client.DefaultRequestHeaders.Add("X-Salesforce-App", "imperial-backend-integration");
        return client;
    }

    private async Task<UserInfo> GetUserProfile(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserInfo>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        return null;
    }

    private async Task<List<OutletSummary>> GetOutletDataForSalesforce(HttpClient client)
    {
        var response = await client.GetAsync("/api/outlets?pageSize=10&sortBy=name");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var pagedResult = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (pagedResult.TryGetProperty("items", out var itemsProperty))
            {
                return JsonSerializer.Deserialize<List<OutletSummary>>(itemsProperty.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<OutletSummary>();
            }
        }
        return new List<OutletSummary>();
    }

    private async Task<OutletDetails> GetOutletById(HttpClient client, Guid outletId)
    {
        var response = await client.GetAsync($"/api/outlets/{outletId}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OutletDetails>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        return null;
    }

    private async Task<SessionRefreshResponse> RefreshSession(HttpClient client)
    {
        var response = await client.PostAsync("/api/auth/refresh-session", 
            new StringContent("{}", Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SessionRefreshResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        return null;
    }

    private async Task<bool> LogoutFromImperialBackend(HttpClient client)
    {
        var response = await client.PostAsync("/api/auth/logout", 
            new StringContent("{}", Encoding.UTF8, "application/json"));
        return response.IsSuccessStatusCode;
    }

    #endregion

    public void Dispose()
    {
        _apiClient?.Dispose();
        _rsaKey?.Dispose();
    }
}

#region Salesforce Simulation Models

public class SalesforceUser
{
    public string Id { get; set; } // Salesforce User ID
    public string Email { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string Department { get; set; }
    public string CompanyName { get; set; }
}

public class SalesforceToImperialRequest
{
    public string AccessToken { get; set; }
    public string Source { get; set; }
    public string SalesforceUserId { get; set; }
    public string SalesforceOrgId { get; set; }
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
}

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

public class OutletSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Tier { get; set; }
    public int Rank { get; set; }
    public string ChainType { get; set; }
    public bool IsActive { get; set; }
}

public class OutletDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Tier { get; set; }
    public int Rank { get; set; }
    public string ChainType { get; set; }
    public decimal Sales { get; set; }
    public string Currency { get; set; }
    public decimal VolumeSoldKg { get; set; }
    public decimal VolumeTargetKg { get; set; }
    public decimal TargetAchievementPercentage { get; set; }
    public Address Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}

public class SessionRefreshResponse
{
    public string SessionToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

#endregion