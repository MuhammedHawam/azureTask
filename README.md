# Azure Outlet Management API

A comprehensive ASP.NET Core Web API solution for managing retail outlets with Azure AD authentication, built using Clean Architecture, CQRS with MediatR, and modern development practices.

## 🏗️ Architecture

This solution follows **Clean Architecture** principles with the following layers:

- **Domain Layer**: Contains business entities, value objects, enums, and interfaces
- **Application Layer**: Contains business logic, CQRS commands/queries, DTOs, and validators
- **Infrastructure Layer**: Contains data access, external service integrations, and cross-cutting concerns
- **API Layer**: Contains controllers, middleware, and API configuration

## 🚀 Features

### Core Features
- ✅ **Clean Architecture** with SOLID principles
- ✅ **CQRS Pattern** with MediatR
- ✅ **Domain-Driven Design** (DDD) with rich domain models
- ✅ **FluentValidation** for request validation
- ✅ **AutoMapper** for object mapping
- ✅ **Entity Framework Core** with SQL Server
- ✅ **Azure AD Authentication** and Authorization
- ✅ **Comprehensive Logging** middleware
- ✅ **Result Pattern** for error handling
- ✅ **Pagination** support
- ✅ **Unit Testing** with xUnit and Moq
- ✅ **Swagger/OpenAPI** documentation

### Business Features
- 🏪 **Outlet Management**: Create, read, update, delete outlets
- 📊 **Performance Tracking**: Sales, volume sold/target tracking
- 📍 **Location Management**: Address-based outlet organization
- 🏆 **Ranking System**: Tier and rank-based outlet classification
- 🔗 **Chain Classification**: Regional vs National chain support
- 📅 **Visit Tracking**: Last visit date and visit scheduling
- 🎯 **Target Achievement**: Performance metrics and reporting

## 🏢 Outlet Properties

Each outlet contains the following information:

- **Name**: Outlet name (required, max 200 chars)
- **Tier**: Business tier classification (required, max 50 chars)
- **Rank**: Numeric ranking (required, > 0)
- **Chain Type**: Regional or National (enum)
- **Last Visit Date**: When the outlet was last visited (optional)
- **Sales**: Sales amount with currency (Money value object)
- **Volume Sold (kg)**: Actual volume sold in kilograms
- **Volume Target (kg)**: Target volume in kilograms
- **Address**: Complete address (Street, City, State, Postal Code, Country)
- **Active Status**: Whether the outlet is currently active

## 📁 Project Structure

```
AzureProductApi/
├── src/
│   ├── AzureProductApi.Domain/           # Domain layer
│   │   ├── Common/                       # Base entities and shared code
│   │   ├── Entities/                     # Domain entities (Outlet)
│   │   ├── ValueObjects/                 # Value objects (Money, Address)
│   │   ├── Enums/                        # Domain enums (ChainType)
│   │   └── Interfaces/                   # Repository interfaces
│   ├── AzureProductApi.Application/      # Application layer
│   │   ├── Common/                       # Shared application code
│   │   ├── DTOs/                         # Data transfer objects
│   │   ├── Outlets/                      # Outlet-specific features
│   │   │   ├── Commands/                 # CQRS commands
│   │   │   └── Queries/                  # CQRS queries
│   │   └── Mappings/                     # AutoMapper profiles
│   ├── AzureProductApi.Infrastructure/   # Infrastructure layer
│   │   ├── Data/                         # EF Core DbContext and configurations
│   │   └── Repositories/                 # Repository implementations
│   └── AzureProductApi.Api/              # API layer
│       ├── Controllers/                  # API controllers
│       └── Middleware/                   # Custom middleware
└── tests/
    └── AzureProductApi.Tests/            # Unit tests
```

## 🛠️ Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Azure AD tenant (for authentication)
- Visual Studio 2022 or VS Code

## ⚙️ Configuration

### 1. Database Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AzureOutletDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  }
}
```

### 2. Azure AD Configuration

Configure Azure AD settings in `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "Audience": "your-audience"
  }
}
```

### 3. Logging Configuration

Serilog is configured for structured logging:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/api-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

## 🚀 Getting Started

### 1. Clone and Setup

```bash
git clone <repository-url>
cd AzureProductApi
dotnet restore
```

### 2. Database Migration

```bash
cd src/AzureProductApi.Api
dotnet ef migrations add InitialCreate --project ../AzureProductApi.Infrastructure
dotnet ef database update --project ../AzureProductApi.Infrastructure
```

### 3. Run the Application

```bash
dotnet run --project src/AzureProductApi.Api
```

The API will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7001/swagger`

## 📚 API Endpoints

### Outlets

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/outlets` | Get outlets with filtering and pagination |
| GET | `/api/outlets/{id}` | Get outlet by ID |
| POST | `/api/outlets` | Create new outlet |
| PUT | `/api/outlets/{id}` | Update outlet |
| DELETE | `/api/outlets/{id}` | Delete outlet |
| GET | `/api/outlets/tiers` | Get all outlet tiers |
| GET | `/api/outlets/cities` | Get all cities |
| GET | `/api/outlets/needing-visit` | Get outlets needing visits |
| GET | `/api/outlets/high-performing` | Get high-performing outlets |

### Query Parameters for GET /api/outlets

- `tier`: Filter by tier
- `chainType`: Filter by chain type (Regional=1, National=2)
- `isActive`: Filter by active status
- `city`: Filter by city
- `state`: Filter by state
- `searchTerm`: Search in name and address
- `minRank`, `maxRank`: Filter by rank range
- `needsVisit`: Filter outlets needing visits
- `highPerforming`: Filter high-performing outlets
- `pageNumber`: Page number (default: 1)
- `pageSize`: Page size (default: 10, max: 100)
- `sortBy`: Sort field (Name, Tier, Rank, etc.)
- `sortDirection`: Sort direction (asc/desc)

### Sample Request Bodies

#### Create Outlet

```json
{
  "name": "Downtown Store",
  "tier": "Premium",
  "rank": 1,
  "chainType": 2,
  "sales": 50000.00,
  "currency": "USD",
  "volumeSoldKg": 1200.50,
  "volumeTargetKg": 1500.00,
  "address": {
    "street": "123 Main Street",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "USA"
  },
  "lastVisitDate": "2024-01-15T10:30:00Z"
}
```

#### Update Outlet

```json
{
  "name": "Downtown Premium Store",
  "tier": "Premium Plus",
  "rank": 1,
  "chainType": 2,
  "sales": 55000.00,
  "currency": "USD",
  "volumeSoldKg": 1350.75,
  "volumeTargetKg": 1600.00,
  "address": {
    "street": "123 Main Street",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "USA"
  },
  "lastVisitDate": "2024-01-20T14:15:00Z"
}
```

## 🔐 Authentication

The API uses Azure AD for authentication. Include the Bearer token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## 🧪 Testing

### Run Unit Tests

```bash
dotnet test
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Monitoring and Logging

### Request Logging

All HTTP requests and responses are logged with:
- Request ID for correlation
- Request/response headers and bodies
- Performance metrics
- User information
- Error details

### Performance Monitoring

- Slow request detection (>5 seconds)
- Database query performance
- Memory usage tracking

## 🔧 Development Guidelines

### Adding New Features

1. **Domain First**: Start with domain entities and business rules
2. **Commands/Queries**: Implement CQRS commands and queries
3. **Validation**: Add FluentValidation rules
4. **Repository**: Extend repository interfaces and implementations
5. **Controllers**: Create API endpoints
6. **Tests**: Write comprehensive unit tests

### Code Quality

- Follow SOLID principles
- Use dependency injection
- Implement proper error handling
- Write meaningful unit tests
- Document public APIs
- Use consistent naming conventions

## 🚀 Deployment

### Azure App Service

1. Create Azure App Service
2. Configure Azure AD authentication
3. Set up Azure SQL Database
4. Configure application settings
5. Deploy using GitHub Actions or Azure DevOps

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AzureProductApi.Api/AzureProductApi.Api.csproj", "src/AzureProductApi.Api/"]
RUN dotnet restore "src/AzureProductApi.Api/AzureProductApi.Api.csproj"
COPY . .
WORKDIR "/src/src/AzureProductApi.Api"
RUN dotnet build "AzureProductApi.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureProductApi.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureProductApi.Api.dll"]
```

## 📝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the documentation
- Review the API examples 
