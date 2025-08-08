using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ImperialBackend.Tests.Manual;

/// <summary>
/// Manual test runner for demonstrating SSO token validation
/// This can be used for manual testing and debugging of the SSO flow
/// </summary>
public class SsoTestRunner
{
    private readonly RSA _rsaKey;
    private readonly string _tenantId = "test-tenant-12345678-1234-5678-9012";
    private readonly string _clientId = "test-client-87654321-4321-8765-2109";
    private readonly string _baseUrl;

    public SsoTestRunner(string baseUrl = "https://localhost:7001")
    {
        _baseUrl = baseUrl;
        _rsaKey = RSA.Create(2048);
    }

    /// <summary>
    /// Demonstrates the complete SSO flow with console output
    /// </summary>
    public async Task RunCompleteDemo()
    {
        Console.WriteLine("🚀 Starting Imperial Backend SSO Integration Demo");
        Console.WriteLine(new string('=', 60));

        try
        {
            // Step 1: Create mock Azure AD token
            Console.WriteLine("\n📋 Step 1: Creating Mock Azure AD Token");
            var mockUser = new MockUser
            {
                Id = "demo-user-123",
                Email = "demo.user@testcompany.com",
                Name = "Demo User",
                Department = "Sales",
                Role = "Sales Manager"
            };

            var azureToken = CreateMockAzureAdToken(mockUser);
            Console.WriteLine($"✅ Created Azure AD token for: {mockUser.Name}");
            Console.WriteLine($"📧 Email: {mockUser.Email}");
            Console.WriteLine($"🎫 Token (first 50 chars): {azureToken[..50]}...");

            // Step 2: Validate token with Imperial Backend API
            Console.WriteLine("\n📋 Step 2: Validating Token with Imperial Backend");
            var ssoResult = await ValidateTokenWithApi(azureToken);
            
            if (ssoResult.IsValid)
            {
                Console.WriteLine($"✅ SSO Validation Success!");
                Console.WriteLine($"👤 User: {ssoResult.User?.Name}");
                Console.WriteLine($"📧 Email: {ssoResult.User?.Email}");
                Console.WriteLine($"🎫 Session Token: {ssoResult.SessionToken?[..30]}...");
                Console.WriteLine($"⏰ Expires: {ssoResult.ExpiresAt}");

                // Step 3: Use session token for API calls
                Console.WriteLine("\n📋 Step 3: Testing API Access with Session Token");
                await TestApiAccess(ssoResult.SessionToken);
            }
            else
            {
                Console.WriteLine($"❌ SSO Validation Failed: {ssoResult.ErrorMessage}");
            }

            // Step 4: Test security scenarios
            Console.WriteLine("\n📋 Step 4: Testing Security Scenarios");
            await TestSecurityScenarios();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n🏁 Demo completed!");
    }

    /// <summary>
    /// Creates a realistic mock Azure AD token for testing
    /// </summary>
    public string CreateMockAzureAdToken(MockUser user, DateTime? expiry = null, bool invalidSignature = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var claims = new List<Claim>
        {
            new("oid", user.Id),
            new("sub", user.Id),
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
            // Additional claims for enriched testing
            new("department", user.Department ?? "Unknown"),
            new("jobTitle", user.Role ?? "User"),
            new("app_displayname", "360 Salesforce")
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

    /// <summary>
    /// Tests the SSO validation endpoint
    /// </summary>
    public async Task<SsoValidationResult> ValidateTokenWithApi(string azureToken)
    {
        try
        {
            using var client = new HttpClient();
            
            var request = new
            {
                accessToken = azureToken,
                source = "mock-test"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_baseUrl}/api/auth/validate-sso", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<SsoValidationResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return result ?? new SsoValidationResult { IsValid = false, ErrorMessage = "Failed to parse response" };
            }
            else
            {
                return new SsoValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new SsoValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = $"Request failed: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// Tests API access with session token
    /// </summary>
    private async Task TestApiAccess(string sessionToken)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);

            // Test 1: Get user info
            Console.WriteLine("  🔸 Testing /api/auth/me endpoint...");
            var userResponse = await client.GetAsync($"{_baseUrl}/api/auth/me");
            if (userResponse.IsSuccessStatusCode)
            {
                var userJson = await userResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"  ✅ User info retrieved successfully");
                Console.WriteLine($"  📄 Response: {userJson[..Math.Min(100, userJson.Length)]}...");
            }
            else
            {
                Console.WriteLine($"  ❌ User info failed: {userResponse.StatusCode}");
            }

            // Test 2: Get outlets
            Console.WriteLine("  🔸 Testing /api/outlets endpoint...");
            var outletsResponse = await client.GetAsync($"{_baseUrl}/api/outlets?pageSize=5");
            if (outletsResponse.IsSuccessStatusCode)
            {
                var outletsJson = await outletsResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"  ✅ Outlets retrieved successfully");
                Console.WriteLine($"  📄 Response length: {outletsJson.Length} characters");
            }
            else
            {
                Console.WriteLine($"  ❌ Outlets failed: {outletsResponse.StatusCode}");
            }

            // Test 3: Refresh session
            Console.WriteLine("  🔸 Testing session refresh...");
            var refreshContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var refreshResponse = await client.PostAsync($"{_baseUrl}/api/auth/refresh-session", refreshContent);
            if (refreshResponse.IsSuccessStatusCode)
            {
                var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"  ✅ Session refreshed successfully");
            }
            else
            {
                Console.WriteLine($"  ❌ Session refresh failed: {refreshResponse.StatusCode}");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ API test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests various security scenarios
    /// </summary>
    private async Task TestSecurityScenarios()
    {
        // Test 1: Expired token
        Console.WriteLine("  🔸 Testing expired token scenario...");
        var expiredUser = new MockUser { Id = "expired-user", Email = "expired@testcompany.com", Name = "Expired User" };
        var expiredToken = CreateMockAzureAdToken(expiredUser, expiry: DateTime.UtcNow.AddHours(-1));
        var expiredResult = await ValidateTokenWithApi(expiredToken);
        
        if (!expiredResult.IsValid)
        {
            Console.WriteLine($"  ✅ Expired token correctly rejected: {expiredResult.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"  ❌ Expired token was incorrectly accepted!");
        }

        // Test 2: Invalid domain
        Console.WriteLine("  🔸 Testing invalid domain scenario...");
        var invalidUser = new MockUser { Id = "invalid-user", Email = "hacker@malicious.com", Name = "Hacker" };
        var invalidToken = CreateMockAzureAdToken(invalidUser);
        var invalidResult = await ValidateTokenWithApi(invalidToken);
        
        if (!invalidResult.IsValid)
        {
            Console.WriteLine($"  ✅ Invalid domain correctly rejected: {invalidResult.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"  ❌ Invalid domain was incorrectly accepted!");
        }

        // Test 3: Invalid signature
        Console.WriteLine("  🔸 Testing invalid signature scenario...");
        var maliciousUser = new MockUser { Id = "malicious-user", Email = "attacker@testcompany.com", Name = "Attacker" };
        var maliciousToken = CreateMockAzureAdToken(maliciousUser, invalidSignature: true);
        var maliciousResult = await ValidateTokenWithApi(maliciousToken);
        
        if (!maliciousResult.IsValid)
        {
            Console.WriteLine($"  ✅ Invalid signature correctly rejected: {maliciousResult.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"  ❌ Invalid signature was incorrectly accepted!");
        }

        // Test 4: Empty token
        Console.WriteLine("  🔸 Testing empty token scenario...");
        var emptyResult = await ValidateTokenWithApi("");
        
        if (!emptyResult.IsValid)
        {
            Console.WriteLine($"  ✅ Empty token correctly rejected: {emptyResult.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"  ❌ Empty token was incorrectly accepted!");
        }
    }

    /// <summary>
    /// Prints the token payload for debugging
    /// </summary>
    public void PrintTokenPayload(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            Console.WriteLine("\n🔍 Token Payload Analysis:");
            Console.WriteLine($"Issuer: {jsonToken.Issuer}");
            Console.WriteLine($"Audience: {string.Join(", ", jsonToken.Audiences)}");
            Console.WriteLine($"Expires: {jsonToken.ValidTo}");
            Console.WriteLine($"Valid From: {jsonToken.ValidFrom}");
            Console.WriteLine($"Algorithm: {jsonToken.Header.Alg}");
            
            Console.WriteLine("\nClaims:");
            foreach (var claim in jsonToken.Claims)
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse token: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _rsaKey?.Dispose();
    }
}

#region Models

public class MockUser
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Role { get; set; } = "";
}

public class SsoValidationResult
{
    public bool IsValid { get; set; }
    public MockUserInfo? User { get; set; }
    public string? SessionToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MockUserInfo
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string GivenName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string TenantId { get; set; } = "";
}

#endregion

/// <summary>
/// Example usage of the SSO test runner
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var testRunner = new SsoTestRunner();
        
        Console.WriteLine("Imperial Backend SSO Mock Test Runner");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("This tool demonstrates how the 360 Salesforce SSO integration works");
        Console.WriteLine("by creating mock Azure AD tokens and testing the validation flow.");
        Console.WriteLine();
        
        if (args.Length > 0 && args[0] == "--demo")
        {
            await testRunner.RunCompleteDemo();
        }
        else
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --demo    # Run complete demo");
            Console.WriteLine();
            Console.WriteLine("Or use the SsoTestRunner class in your own tests:");
            Console.WriteLine();
            Console.WriteLine("```csharp");
            Console.WriteLine("var runner = new SsoTestRunner(\"https://your-api-url\");");
            Console.WriteLine("var user = new MockUser { Email = \"test@company.com\", Name = \"Test User\" };");
            Console.WriteLine("var token = runner.CreateMockAzureAdToken(user);");
            Console.WriteLine("var result = await runner.ValidateTokenWithApi(token);");
            Console.WriteLine("```");
        }
        
        testRunner.Dispose();
    }
}

