# SSO Integration Guide - Imperial Backend API (Databricks Edition)

## Option 1: Use Azure AD SSO (Best Practice)

Since both apps are under Azure AD authentication, leverage OpenID Connect and SSO session management.

### 📌 Flow:

1. User logs into Salesforce using Azure AD → gets ID Token + Access Token.

2. When user navigates to App X, it receives a JWT token or SSO context (possibly via redirect with token).

3. App X uses this token to:
   - Validate it (public key from Azure AD).
   - Extract user identity (email, sub, roles).
   - Log the user in without asking for credentials again.

### 🛠 Implementation in App X:

- Use Microsoft.Identity.Web or OpenIdConnectAuthentication in your .NET backend.
- Trust Azure AD as your Identity Provider (IdP).
- Configure App X as an Azure AD app registration under the same tenant as Salesforce app.
- App X receives token from frontend (either via redirect from Salesforce or iframe message)

## 🔥 **NEW: Azure Databricks Integration Options**

You asked a great question: **"Can LINQ queries or EF queries be done with Databricks instead of Dapper?"**

**Answer: YES! You have multiple options:**

### ✅ **Option 1: Entity Framework Core + LINQ (Recommended)**

**Pros:**
- Keep all your existing LINQ queries
- Full Entity Framework Core functionality
- Familiar development experience
- Strong typing and IntelliSense
- Automatic query optimization

**Implementation:**
```csharp
// Using CData Databricks Entity Framework Core Provider
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseCData("Databricks", connectionString);
    // OR alternatively:
    // options.UseProvider("CData.Databricks", connectionString);
});

// Your existing LINQ queries work exactly the same:
var outlets = await context.Outlets
    .Where(o => o.IsActive && o.Tier == "Premium")
    .OrderBy(o => o.Name)
    .ToListAsync();
```

**Package Required:**
```xml
<PackageReference Include="CData.Databricks.EntityFrameworkCore" Version="24.0.9175" />
```

### ✅ **Option 2: Dapper + Raw SQL (What we implemented)**

**Pros:**
- Full control over SQL queries
- Better performance for complex queries
- Direct Spark SQL optimization
- Lower memory footprint

**Implementation:**
```csharp
// Raw SQL with Dapper
var sql = @"
    SELECT * FROM outlets 
    WHERE IsActive = true AND Tier = @tier 
    ORDER BY Name";

var outlets = await connection.QueryAsync<OutletDataModel>(sql, new { tier = "Premium" });
```

### ✅ **Option 3: Hybrid Approach (Best of Both Worlds)**

**Use Entity Framework for:**
- Simple CRUD operations
- Standard filtering and sorting
- Relationship navigation
- Development productivity

**Use Dapper for:**
- Complex analytics queries
- Custom Spark SQL optimizations
- Bulk operations
- Performance-critical queries

### 📊 **Comparison Table:**

| Feature | Entity Framework + LINQ | Dapper + Raw SQL |
|---------|-------------------------|------------------|
| **Development Speed** | ✅ Fast (familiar LINQ) | ⚠️ Slower (write SQL) |
| **Query Performance** | ⚠️ Good (auto-optimized) | ✅ Excellent (hand-tuned) |
| **Type Safety** | ✅ Full compile-time | ⚠️ Runtime only |
| **Complex Queries** | ⚠️ Limited by LINQ | ✅ Full Spark SQL |
| **Maintenance** | ✅ Easy to maintain | ⚠️ More complex |
| **Learning Curve** | ✅ Minimal | ⚠️ Requires SQL knowledge |

### 🔧 **Technical Implementation Details:**

#### **Entity Framework Core Approach:**
```csharp
// Your existing repository methods work unchanged:
public async Task<IEnumerable<Outlet>> GetAllAsync(
    string? tier = null,
    ChainType? chainType = null,
    bool? isActive = null,
    // ... other parameters
    CancellationToken cancellationToken = default)
{
    var query = _context.Outlets.AsNoTracking();

    // All your existing LINQ filters work:
    if (!string.IsNullOrWhiteSpace(tier))
        query = query.Where(o => o.Tier == tier);

    if (chainType.HasValue)
        query = query.Where(o => o.ChainType == chainType.Value);

    if (isActive.HasValue)
        query = query.Where(o => o.IsActive == isActive.Value);

    // Sorting and pagination work exactly the same:
    query = query.OrderBy(o => o.Name).Skip(skip).Take(pageSize);

    return await query.ToListAsync(cancellationToken);
}
```

#### **Dapper Approach (Current Implementation):**
```csharp
// Direct SQL control for Spark SQL optimization:
var sql = $@"
    SELECT * FROM {_tableName} 
    WHERE IsActive = @isActive 
      AND Tier = @tier
      AND (VolumeSoldKg / VolumeTargetKg * 100) >= @minAchievement
    ORDER BY (VolumeSoldKg / VolumeTargetKg) DESC
    LIMIT @pageSize OFFSET @skip";

var results = await connection.QueryAsync<OutletDataModel>(sql, parameters);
```

### ⚙️ **Configuration Options:**

#### **Entity Framework Core Configuration:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-workspace.databricks.com;HTTPPath=/sql/1.0/warehouses/your-id;AuthScheme=Token;Token=your-token;UseSSL=True;Port=443;"
  }
}
```

#### **Dapper Configuration:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Driver={Simba Spark ODBC Driver};Host=your-workspace.databricks.com;Port=443;HTTPPath=/sql/1.0/warehouses/your-id;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=your-token;"
  }
}
```

### 🚀 **My Recommendation:**

**Start with Entity Framework Core + LINQ** because:

1. **Zero Code Changes**: Your existing LINQ queries work immediately
2. **Faster Development**: No need to rewrite queries as SQL
3. **Better Maintainability**: Type-safe, compile-time checked queries
4. **Gradual Migration**: You can always add Dapper for specific performance-critical queries later

**Switch to Dapper only if:**
- You need complex Spark SQL features not supported by LINQ
- Performance is critical and you want hand-tuned queries
- You're comfortable writing and maintaining raw SQL

### 📋 **Required Setup for Entity Framework Approach:**

1. **Install CData Provider:**
   ```bash
   dotnet add package CData.Databricks.EntityFrameworkCore
   ```

2. **Update Program.cs:**
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseCData("Databricks", connectionString));
   ```

3. **Your existing LINQ queries work unchanged!**

## API Integration with Salesforce Frontend

### Outlet API Endpoint

From the Salesforce app, the frontend will call specific endpoint for calling the outlet by ID in the request and share the response with the FE in order to display it in the application.

**Example Request:**
```
GET https://api/outlet/getoutletById/1
```

**Response:**
The response will contain the complete outlet model and properties for display in the Salesforce application.

### Current API Endpoints Available

#### Get Outlet by ID
```http
GET /api/outlets/{id}
```

**Response Model (OutletDto):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "string",
  "tier": "string",
  "rank": 0,
  "chainType": "Regional",
  "sales": {
    "amount": 0,
    "currency": "string"
  },
  "volumeSoldKg": 0,
  "volumeTargetKg": 0,
  "targetAchievementPercentage": 0,
  "address": {
    "street": "string",
    "city": "string",
    "state": "string",
    "zipCode": "string",
    "country": "string"
  },
  "isActive": true,
  "lastVisitDate": "2024-01-01T00:00:00Z",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

#### Additional Available Endpoints

##### Get All Outlets with Filtering (Databricks-Optimized)
```http
GET /api/outlets?tier=Premium&chainType=National&isActive=true&pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc
```

**Query Parameters:**
- `tier` - Filter by outlet tier
- `chainType` - Filter by chain type (Regional=1, National=2)
- `isActive` - Filter by active status
- `city` - Filter by city name
- `state` - Filter by state
- `searchTerm` - Search in name and address fields
- `minRank` / `maxRank` - Filter by rank range
- `needsVisit` - Filter outlets needing visits
- `maxDaysSinceVisit` - Days since last visit (default: 30)
- `highPerforming` - Filter high-performing outlets
- `minAchievementPercentage` - Minimum achievement % (default: 80)
- `pageNumber` - Page number (default: 1)
- `pageSize` - Page size (default: 10, max: 100)
- `sortBy` - Sort field (name, tier, rank, sales, etc.)
- `sortDirection` - Sort direction (asc/desc)

##### Get Outlets Needing Visits
```http
GET /api/outlets/needing-visit?maxDaysSinceVisit=30&pageNumber=1&pageSize=10
```

##### Get High-Performing Outlets
```http
GET /api/outlets/high-performing?minAchievementPercentage=80&pageNumber=1&pageSize=10
```

##### Get Available Filter Options
```http
GET /api/outlets/tiers      # Returns list of available tiers
GET /api/outlets/cities     # Returns list of available cities
```

### Authentication Requirements

All API endpoints require Azure AD authentication. Include the Bearer token in the Authorization header:

```http
Authorization: Bearer {azure-ad-jwt-token}
```

### Error Handling

The API returns standard HTTP status codes:
- `200 OK` - Successful request
- `400 Bad Request` - Invalid parameters
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - Outlet not found
- `500 Internal Server Error` - Server error

### Health Monitoring

Check API and Databricks connectivity:
```http
GET /health
```

Returns health status including Databricks connection state.

### Performance Optimizations

🚀 **Databricks-Specific Optimizations:**

**With Entity Framework Core:**
- **LINQ Translation**: EF Core translates LINQ to optimized Spark SQL
- **AsNoTracking()**: Read-only queries for better performance
- **Compiled Queries**: Pre-compiled LINQ for repeated queries
- **Query Splitting**: Automatic optimization for complex joins

**With Dapper:**
- **Raw Spark SQL**: Hand-tuned queries for maximum performance
- **Columnar Storage**: Direct Delta Lake optimizations
- **Custom Indexing**: Leverage Databricks-specific indexes
- **Batch Operations**: Bulk inserts and updates

### CORS Configuration

The API is configured to accept requests from authorized frontend applications. Ensure your Salesforce app domain is added to the CORS policy if needed.