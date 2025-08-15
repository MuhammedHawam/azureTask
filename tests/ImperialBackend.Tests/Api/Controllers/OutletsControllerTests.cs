using AutoMapper;
using ImperialBackend.Api.Controllers;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Application.Outlets.Queries.GetOutlets;
using ImperialBackend.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ImperialBackend.Tests.Api.Controllers;

public class OutletsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<OutletsController>> _mockLogger;
    private readonly Mock<IOutletRepository> _mockRepository;
    private readonly OutletsController _controller;

    public OutletsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<OutletsController>>();
        _mockRepository = new Mock<IOutletRepository>();
        
        _controller = new OutletsController(_mockMediator.Object, _mockMapper.Object, _mockLogger.Object, _mockRepository.Object);
        
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
                Year = 2019,
                Week = 23,
                TotalOuterQuantity = 1,
                CountOuterQuantity = 1,
                TotalSales6w = 3,
                Mean = 14.08m,
                LowerLimit = 4,
                UpperLimit = 18,
                HealthStatus = "red",
                StoreRank = 173,
                OutletName = "OMEGNA 0002",
                OutletIdentifier = "001w000001ZUPO8AAP",
                AddressLine1 = "PIAZZA BELTRAMI 21",
                State = "VB",
                County = "PIEMONTE"
            }
        };
        
        var pagedResult = new PagedResult<OutletDto>(outlets, 1, 1, 10);
        var result = Result<PagedResult<OutletDto>>.Success(pagedResult);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOutletsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var query = new GetOutletsQuery { Year = 2019, Week = 23, HealthStatus = "red" };
        var response = await _controller.GetOutlets(query);

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
        var query = new GetOutletsQuery();
        var response = await _controller.GetOutlets(query);

        // Assert
        response.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = response.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid page size");
    }

    [Fact]
    public async Task GetOutlet_ByIdentifier_ShouldReturnOk()
    {
        // Arrange
        var outletIdentifier = "001w000001ZUPO8AAP";
        var outletEntity = new ImperialBackend.Domain.Entities.Outlet(2019, 23, 1, 1, 3, 14.08m, 4, 18, "red", 173, "OMEGNA 0002", outletIdentifier, "PIAZZA BELTRAMI 21", "VB", "PIEMONTE");
        var outletDto = new OutletDto { OutletIdentifier = outletIdentifier };

        _mockRepository.Setup(r => r.GetByIdAsync(outletIdentifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outletEntity);
        _mockMapper.Setup(m => m.Map<OutletDto>(outletEntity)).Returns(outletDto);

        // Act
        var response = await _controller.GetOutlet(outletIdentifier);

        // Assert
        response.Result.Should().BeOfType<OkObjectResult>();
        var ok = response.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(outletDto);
    }

    [Fact]
    public async Task CreateOutlet_WithValidCommand_ShouldReturnCreatedResult()
    {
        // Arrange
        var command = new CreateOutletCommand
        {
            Year = 2019,
            Week = 23,
            TotalOuterQuantity = 1,
            CountOuterQuantity = 1,
            TotalSales6w = 3,
            Mean = 14.08m,
            LowerLimit = 4,
            UpperLimit = 18,
            HealthStatus = "red",
            StoreRank = 173,
            OutletName = "OMEGNA 0002",
            OutletIdentifier = "001w000001ZUPO8AAP",
            AddressLine1 = "PIAZZA BELTRAMI 21",
            State = "VB",
            County = "PIEMONTE",
            UserId = "test-user-id"
        };

        var createdOutlet = new OutletDto
        {
            OutletIdentifier = command.OutletIdentifier,
            OutletName = command.OutletName
        };

        var result = Result<OutletDto>.Success(createdOutlet);

        _mockMediator.Setup(m => m.Send(It.Is<CreateOutletCommand>(c => c.UserId == "test-user-id"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateOutlet(command);

        // Assert
        response.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = response.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdOutlet);
        createdResult.ActionName.Should().Be(nameof(OutletsController.GetOutlet));
        createdResult.RouteValues!["outletIdentifier"].Should().Be(command.OutletIdentifier);
    }
}