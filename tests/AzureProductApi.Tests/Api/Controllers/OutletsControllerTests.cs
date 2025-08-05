using AutoMapper;
using AzureProductApi.Api.Controllers;
using AzureProductApi.Application.Common.Models;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Application.Outlets.Commands.CreateOutlet;
using AzureProductApi.Application.Outlets.Queries.GetOutlets;
using AzureProductApi.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AzureProductApi.Tests.Api.Controllers;

public class OutletsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<OutletsController>> _mockLogger;
    private readonly OutletsController _controller;

    public OutletsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<OutletsController>>();
        
        _controller = new OutletsController(_mockMediator.Object, _mockMapper.Object, _mockLogger.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new("name", "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetOutlets_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        var outlets = new List<OutletDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Outlet 1",
                Tier = "Premium",
                Rank = 1,
                ChainType = ChainType.National
            }
        };
        
        var pagedResult = new PagedResult<OutletDto>(outlets, 1, 1, 10);
        var result = Result<PagedResult<OutletDto>>.Success(pagedResult);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOutletsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetOutlets();

        // Assert
        response.Result.Should().BeOfType<OkObjectResult>();
        var okResult = response.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetOutlets_WithFailedResult_ShouldReturnBadRequest()
    {
        // Arrange
        var result = Result<PagedResult<OutletDto>>.Failure("Invalid page size");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOutletsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetOutlets();

        // Assert
        response.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = response.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid page size");
    }

    [Fact]
    public async Task CreateOutlet_WithValidDto_ShouldReturnCreatedResult()
    {
        // Arrange
        var createDto = new CreateOutletDto
        {
            Name = "New Outlet",
            Tier = "Premium",
            Rank = 1,
            ChainType = ChainType.National,
            Sales = 10000m,
            Currency = "USD",
            VolumeSoldKg = 500m,
            VolumeTargetKg = 1000m,
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        var command = new CreateOutletCommand
        {
            Name = createDto.Name,
            Tier = createDto.Tier,
            Rank = createDto.Rank,
            ChainType = createDto.ChainType,
            UserId = "test-user-id"
        };

        var createdOutlet = new OutletDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Tier = createDto.Tier,
            Rank = createDto.Rank,
            ChainType = createDto.ChainType
        };

        var result = Result<OutletDto>.Success(createdOutlet);

        _mockMapper.Setup(m => m.Map<CreateOutletCommand>(createDto))
            .Returns(command);

        _mockMediator.Setup(m => m.Send(It.Is<CreateOutletCommand>(c => c.UserId == "test-user-id"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateOutlet(createDto);

        // Assert
        response.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = response.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdOutlet);
        createdResult.ActionName.Should().Be(nameof(OutletsController.GetOutlet));
    }

    [Fact]
    public async Task CreateOutlet_WithDuplicateOutlet_ShouldReturnConflict()
    {
        // Arrange
        var createDto = new CreateOutletDto
        {
            Name = "Existing Outlet",
            Tier = "Premium",
            Rank = 1,
            ChainType = ChainType.National,
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        var command = new CreateOutletCommand();
        var result = Result<OutletDto>.Failure("Outlet 'Existing Outlet' already exists in New York, NY");

        _mockMapper.Setup(m => m.Map<CreateOutletCommand>(createDto))
            .Returns(command);

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateOutletCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateOutlet(createDto);

        // Assert
        response.Result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = response.Result as ConflictObjectResult;
        conflictResult!.Value.Should().Be(result.Error);
    }

    [Fact]
    public async Task CreateOutlet_WithValidationError_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateOutletDto
        {
            Name = "", // Invalid empty name
            Tier = "Premium",
            Rank = 1,
            ChainType = ChainType.National
        };

        var command = new CreateOutletCommand();
        var result = Result<OutletDto>.Failure("Outlet name cannot be empty");

        _mockMapper.Setup(m => m.Map<CreateOutletCommand>(createDto))
            .Returns(command);

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateOutletCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateOutlet(createDto);

        // Assert
        response.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = response.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(result.Error);
    }

    [Fact]
    public async Task GetOutletsNeedingVisit_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        var outlets = new List<OutletDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Outlet Needing Visit",
                Tier = "Premium",
                Rank = 1,
                ChainType = ChainType.National,
                LastVisitDate = DateTime.UtcNow.AddDays(-35) // Needs visit
            }
        };
        
        var pagedResult = new PagedResult<OutletDto>(outlets, 1, 1, 10);
        var result = Result<PagedResult<OutletDto>>.Success(pagedResult);

        _mockMediator.Setup(m => m.Send(It.Is<GetOutletsQuery>(q => 
            q.NeedsVisit == true && 
            q.MaxDaysSinceVisit == 30 &&
            q.SortBy == "LastVisitDate" &&
            q.SortDirection == "asc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetOutletsNeedingVisit(30, 1, 10);

        // Assert
        response.Result.Should().BeOfType<OkObjectResult>();
        var okResult = response.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetHighPerformingOutlets_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        var outlets = new List<OutletDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "High Performing Outlet",
                Tier = "Premium",
                Rank = 1,
                ChainType = ChainType.National,
                VolumeSoldKg = 1500m,
                VolumeTargetKg = 1000m // 150% achievement
            }
        };
        
        var pagedResult = new PagedResult<OutletDto>(outlets, 1, 1, 10);
        var result = Result<PagedResult<OutletDto>>.Success(pagedResult);

        _mockMediator.Setup(m => m.Send(It.Is<GetOutletsQuery>(q => 
            q.HighPerforming == true && 
            q.MinAchievementPercentage == 120 &&
            q.SortBy == "TargetAchievement" &&
            q.SortDirection == "desc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetHighPerformingOutlets(120, 1, 10);

        // Assert
        response.Result.Should().BeOfType<OkObjectResult>();
        var okResult = response.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Theory]
    [InlineData("tier", ChainType.Regional, true, "New York", "NY")]
    [InlineData(null, null, null, null, null)]
    public async Task GetOutlets_WithDifferentFilters_ShouldPassCorrectParameters(
        string? tier, ChainType? chainType, bool? isActive, string? city, string? state)
    {
        // Arrange
        var pagedResult = new PagedResult<OutletDto>(new List<OutletDto>(), 0, 1, 10);
        var result = Result<PagedResult<OutletDto>>.Success(pagedResult);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOutletsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _controller.GetOutlets(tier, chainType, isActive, city, state);

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<GetOutletsQuery>(q =>
            q.Tier == tier &&
            q.ChainType == chainType &&
            q.IsActive == isActive &&
            q.City == city &&
            q.State == state), It.IsAny<CancellationToken>()), Times.Once);
    }
}