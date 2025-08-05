# Imperial Backend API

A comprehensive .NET 8 Web API for managing retail outlets with Azure AD authentication, built using Clean Architecture principles.

## 🏗️ Architecture

This project follows Clean Architecture with the following layers:

```
ImperialBackend/
├── src/
│   ├── ImperialBackend.Domain/           # Domain layer
│   │   ├── Entities/                     # Business entities
│   │   ├── ValueObjects/                 # Value objects
│   │   ├── Enums/                        # Domain enums
│   │   └── Interfaces/                   # Repository interfaces
│   ├── ImperialBackend.Application/      # Application layer
│   │   ├── Common/                       # Shared application logic
│   │   ├── DTOs/                         # Data Transfer Objects
│   │   └── Outlets/                      # Outlet-specific features
│   ├── ImperialBackend.Infrastructure/   # Infrastructure layer
│   │   ├── Data/                         # Entity Framework DbContext
│   │   └── Repositories/                 # Repository implementations
│   └── ImperialBackend.Api/              # API layer
│       ├── Controllers/                  # API controllers
│       └── Middleware/                   # Custom middleware
└── tests/
    └── ImperialBackend.Tests/            # Unit tests
```

## 🚀 Features

### Outlet Management
- ✅ Create, read, update, and delete outlets
- ✅ Search outlets by name, location, and other criteria
- ✅ Filter outlets by tier, chain type, and status
- ✅ Track outlet visits and performance metrics
- ✅ Manage outlet sales targets and achievements

### Technical Features
- ✅ **Clean Architecture** with clear separation of concerns
- ✅ **CQRS Pattern** using MediatR
- ✅ **Azure AD Authentication** for secure access
- ✅ **Entity Framework Core** with SQL Server
- ✅ **AutoMapper** for object mapping
- ✅ **FluentValidation** for request validation
- ✅ **Swagger/OpenAPI** documentation
- ✅ **Structured Logging** with Serilog
- ✅ **Health Checks** for monitoring
- ✅ **CORS** support for frontend integration

## 🛠️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server LocalDB
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd ImperialBackend
```

### 2. Setup Database
```bash
cd src/ImperialBackend.Api
dotnet ef migrations add InitialCreate --project ../ImperialBackend.Infrastructure
dotnet ef database update --project ../ImperialBackend.Infrastructure
```

### 3. Configure Azure AD
Update `appsettings.json` with your Azure AD configuration:
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

### 4. Run the Application
```bash
dotnet run --project src/ImperialBackend.Api
```

The API will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7001/swagger`

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 API Endpoints

### Outlets
- `GET /api/outlets` - Get all outlets with filtering and pagination
- `GET /api/outlets/{id}` - Get outlet by ID
- `POST /api/outlets` - Create a new outlet
- `PUT /api/outlets/{id}` - Update an outlet
- `DELETE /api/outlets/{id}` - Delete an outlet
- `POST /api/outlets/{id}/visit` - Record a visit to an outlet
- `GET /api/outlets/tiers` - Get available outlet tiers
- `GET /api/outlets/cities` - Get cities with outlets

### Health Checks
- `GET /health` - Application health status

## 🐳 Docker Support

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ImperialBackend.Api/ImperialBackend.Api.csproj", "src/ImperialBackend.Api/"]
COPY ["src/ImperialBackend.Application/ImperialBackend.Application.csproj", "src/ImperialBackend.Application/"]
COPY ["src/ImperialBackend.Domain/ImperialBackend.Domain.csproj", "src/ImperialBackend.Domain/"]
COPY ["src/ImperialBackend.Infrastructure/ImperialBackend.Infrastructure.csproj", "src/ImperialBackend.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/ImperialBackend.Api/ImperialBackend.Api.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/ImperialBackend.Api"
RUN dotnet build "ImperialBackend.Api.csproj" -c Release -o /app/build

# Publish application
RUN dotnet publish "ImperialBackend.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ImperialBackend.Api.dll"]
```

## 🔧 Configuration

### Database Connection
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ImperialBackendDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  }
}
```

### CORS Configuration
Configure allowed origins in `appsettings.json`:
```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://localhost:3000"
  ]
}
```

## 🔒 Security

- **Azure AD Integration**: Secure authentication using Azure Active Directory
- **JWT Bearer Tokens**: API endpoints protected with JWT authentication
- **HTTPS Enforcement**: All communications encrypted in production
- **Input Validation**: Comprehensive request validation using FluentValidation

## 📈 Monitoring & Logging

- **Structured Logging**: Using Serilog with configurable log levels
- **Health Checks**: Built-in health monitoring endpoints
- **Performance Tracking**: Request/response logging middleware
- **Error Handling**: Global exception handling with detailed logging

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/new-feature`
3. Commit your changes: `git commit -am 'Add new feature'`
4. Push to the branch: `git push origin feature/new-feature`
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

For support and questions, please contact the development team at dev@company.com. 
