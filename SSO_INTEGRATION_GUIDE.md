# SSO Integration Guide - Imperial Backend API

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

##### Get All Outlets with Filtering
```http
GET /api/outlets?tier=Premium&chainType=National&isActive=true&pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc
```

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

### CORS Configuration

The API is configured to accept requests from authorized frontend applications. Ensure your Salesforce app domain is added to the CORS policy if needed.