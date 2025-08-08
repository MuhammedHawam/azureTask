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

## 🔥 **Azure Databricks Integration - SIMPLIFIED APPROACH**

**You asked: "Why do we need CData package?"**

## ❌ **We DON'T need CData! Here's why:**

### **Problems with CData:**
- 💰 **Commercial License Required** - Not free for production
- 🔧 **Complex Setup** - License activation needed  
- 📚 **Limited Documentation** - Hard to find examples
- 🐛 **Build Errors** - Unclear extension methods
- 🚫 **Unnecessary Complexity** - Microsoft has better solutions

## ✅ **BETTER APPROACH: Microsoft's Official Solution**

**Databricks SQL Warehouses are compatible with SQL Server drivers!**

### **🎯 What We Implemented (WORKING NOW):**

```csharp
// Use Microsoft's official SQL Server provider - works with Databricks!
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    // Your existing LINQ queries work unchanged!
});
```

### **📦 Simple Package Requirements:**
```xml
<!-- Only Microsoft packages - no third-party needed! -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.1" />
```

### **⚙️ Connection String Format:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-databricks-workspace.cloud.databricks.com,443;Database=your-catalog.your-schema;UID=token;PWD=your-databricks-token;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
  }
}
```

## 🚀 **Why This Approach is MUCH BETTER:**

### **✅ Advantages:**
1. **🆓 Completely Free** - No commercial licenses
2. **🏗️ Uses Microsoft Stack** - Official, supported, reliable
3. **🔄 Zero Code Changes** - Your LINQ queries work unchanged
4. **📚 Excellent Documentation** - Full Microsoft docs available
5. **🎯 Proven Technology** - SQL Server provider is mature and stable
6. **🛠️ Easy Setup** - Standard Entity Framework configuration
7. **🔧 Great Tooling** - Full Visual Studio support

### **🎯 Your LINQ Queries Work Perfectly:**
```csharp
// All your existing LINQ patterns work with Databricks:
var outlets = await _context.Outlets
    .AsNoTracking()
    .Where(o => o.IsActive && o.Tier == "Premium")
    .Where(o => o.ChainType == ChainType.National)
    .OrderBy(o => o.Name)
    .Skip(skip)
    .Take(pageSize)
    .ToListAsync();

// Complex filtering still works:
var highPerformers = await _context.Outlets
    .Where(o => o.IsActive && o.VolumeTargetKg > 0 && 
               (o.VolumeSoldKg / o.VolumeTargetKg * 100) >= 80)
    .OrderByDescending(o => o.VolumeSoldKg / o.VolumeTargetKg)
    .ToListAsync();
```

## 📊 **Final Comparison:**

| Feature | Microsoft SQL Server Provider ✅ | CData Provider ❌ |
|---------|----------------------------------|-------------------|
| **Cost** | ✅ **FREE** | ❌ Commercial license required |
| **Setup** | ✅ **Simple** | ❌ Complex license activation |
| **Documentation** | ✅ **Excellent** | ❌ Limited |
| **Build Issues** | ✅ **Works perfectly** | ❌ Extension method errors |
| **LINQ Support** | ✅ **Full support** | ❌ Unclear implementation |
| **Microsoft Support** | ✅ **Official support** | ❌ Third-party |
| **Maintenance** | ✅ **Easy** | ❌ Complex |

## 🎯 **How Databricks SQL Warehouse Works with SQL Server Driver:**

Databricks SQL Warehouses provide **SQL Server-compatible endpoints**:

1. **Protocol Compatibility**: Uses TDS (Tabular Data Stream) protocol
2. **SQL Syntax**: Supports standard SQL Server syntax
3. **Authentication**: Token-based authentication works seamlessly
4. **Performance**: Optimized for analytical workloads
5. **Scaling**: Auto-scaling compute resources

## 🔧 **Technical Implementation:**

### **Entity Framework Core Setup:**
```csharp
// Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

### **Repository Implementation (Unchanged):**
```csharp
// Your existing LINQ repository methods work perfectly:
public async Task<IEnumerable<Outlet>> GetAllAsync(/* parameters */)
{
    var query = _context.Outlets.AsNoTracking();
    
    // All your existing filters work:
    if (!string.IsNullOrWhiteSpace(tier))
        query = query.Where(o => o.Tier == tier);
    
    if (chainType.HasValue)
        query = query.Where(o => o.ChainType == chainType.Value);
    
    // Sorting and pagination work:
    query = query.OrderBy(o => o.Name).Skip(skip).Take(pageSize);
    
    return await query.ToListAsync(cancellationToken);
}
```

### **Connection String Configuration:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-workspace.cloud.databricks.com,443;Database=catalog.schema;UID=token;PWD=dapi1234567890;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
  },
  "Databricks": {
    "ServerHostname": "your-workspace.cloud.databricks.com",
    "HTTPPath": "/sql/1.0/warehouses/your-warehouse-id",
    "AccessToken": "dapi1234567890",
    "Catalog": "main",
    "Schema": "default"
  }
}
```

## 📋 **Setup Instructions:**

### **1. Install Required Packages:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### **2. Configure Connection String:**
- Replace `your-workspace` with your Databricks workspace URL
- Replace `catalog.schema` with your actual catalog and schema
- Replace `dapi1234567890` with your Databricks personal access token

### **3. Your Code Works Unchanged!**
- All existing LINQ queries work
- All Entity Framework features available
- Full type safety and IntelliSense

## 🚀 **Performance Optimizations:**

**Entity Framework + Databricks SQL Warehouse:**
- **AsNoTracking()**: Read-only queries for better performance
- **Query Translation**: EF Core translates LINQ to optimized SQL
- **Connection Pooling**: Automatic connection management
- **Compiled Queries**: Pre-compiled LINQ for repeated queries
- **Spark SQL Engine**: Databricks optimizes queries automatically

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

### CORS Configuration

The API is configured to accept requests from authorized frontend applications. Ensure your Salesforce app domain is added to the CORS policy if needed.