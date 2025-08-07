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

## 🔥 **NEW: Azure Databricks Integration**

The Imperial Backend API has been upgraded to use **Azure Databricks** as the data source instead of traditional SQL Server. This provides:

### ✅ **Databricks Benefits:**
- **Big Data Processing**: Handle large datasets efficiently
- **Real-time Analytics**: Query data with Spark SQL performance
- **Unified Analytics**: Single platform for data engineering and analytics
- **Scalability**: Auto-scaling compute resources
- **Cost Optimization**: Pay only for compute resources used

### 🔧 **Technical Implementation:**
- **Connection**: ODBC driver with Databricks SQL Warehouse
- **Query Engine**: Dapper ORM with raw SQL queries optimized for Spark SQL
- **Performance**: Database-level filtering, sorting, and pagination
- **Health Monitoring**: Built-in health checks for Databricks connectivity

### ⚙️ **Configuration Required:**

Update your `appsettings.json` with Databricks connection details:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Driver={Simba Spark ODBC Driver};Host=your-databricks-workspace.cloud.databricks.com;Port=443;HTTPPath=/sql/1.0/warehouses/your-warehouse-id;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=your-databricks-token;"
  },
  "Databricks": {
    "ServerHostname": "your-databricks-workspace.cloud.databricks.com",
    "HTTPPath": "/sql/1.0/warehouses/your-warehouse-id",
    "AccessToken": "your-databricks-token",
    "Catalog": "your-catalog-name",
    "Schema": "your-schema-name"
  }
}
```

### 📋 **Required Setup:**
1. **Databricks Workspace**: Create or use existing Azure Databricks workspace
2. **SQL Warehouse**: Set up a SQL Warehouse for query execution
3. **Access Token**: Generate personal access token or use service principal
4. **Catalog & Schema**: Ensure your data is organized in Unity Catalog
5. **Table Structure**: Create `outlets` table with required schema

### 🗄️ **Expected Table Schema:**

```sql
CREATE TABLE your_catalog.your_schema.outlets (
    Id STRING,
    Name STRING,
    Tier STRING,
    Rank INT,
    ChainType INT,
    SalesAmount DECIMAL(18,2),
    SalesCurrency STRING,
    VolumeSoldKg DECIMAL(18,2),
    VolumeTargetKg DECIMAL(18,2),
    AddressStreet STRING,
    AddressCity STRING,
    AddressState STRING,
    AddressZipCode STRING,
    AddressCountry STRING,
    IsActive BOOLEAN,
    LastVisitDate TIMESTAMP,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
```

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
- **Spark SQL Queries**: All filtering and sorting executed at Databricks level
- **Columnar Storage**: Efficient data retrieval with Delta Lake format
- **Caching**: Databricks caches frequently accessed data automatically
- **Parallel Processing**: Queries leverage Spark's distributed computing
- **Adaptive Query Execution**: Spark optimizes queries based on data statistics

### CORS Configuration

The API is configured to accept requests from authorized frontend applications. Ensure your Salesforce app domain is added to the CORS policy if needed.