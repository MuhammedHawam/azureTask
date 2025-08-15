using AutoMapper;
using ImperialBackend.Application.Common.Mappings;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ImperialBackend.Tests.Application.Commands;

public class CreateOutletCommandHandlerTests
{
    private readonly Mock<IOutletRepository> _mockRepository;
    private readonly IMapper _mapper;
    private readonly Mock<ILogger<CreateOutletCommandHandler>> _mockLogger;
    private readonly CreateOutletCommandHandler _handler;

    public CreateOutletCommandHandlerTests()
    {
        _mockRepository = new Mock<IOutletRepository>();
        _mockLogger = new Mock<ILogger<CreateOutletCommandHandler>>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _handler = new CreateOutletCommandHandler(_mockRepository.Object, _mapper, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOutlet()
    {
        // Arrange
        var command = new CreateOutletCommand
        {
            Year = 2019,
            Week = 23,
            TotalOuterQuantity = 1,
            CountOuterQuantity = 1,
            TotalSales6w = 3,
            Mean = 14.087557603686635m,
            LowerLimit = 4,
            UpperLimit = 18,
            HealthStatus = "red",
            StoreRank = 173,
            OutletName = "OMEGNA 0002",
            OutletIdentifier = "001w000001ZUPO8AAP",
            AddressLine1 = "PIAZZA BELTRAMI 21",
            State = "VB",
            County = "PIEMONTE",
            UserId = "tester"
        };

        Outlet? added = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Outlet outlet, CancellationToken _) =>
            {
                added = outlet;
                return outlet;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OutletIdentifier.Should().Be(command.OutletIdentifier);
        result.Value.OutletName.Should().Be(command.OutletName);
        result.Value.Year.Should().Be(command.Year);
        result.Value.Week.Should().Be(command.Week);
        result.Value.TotalOuterQuantity.Should().Be(command.TotalOuterQuantity);
        result.Value.CountOuterQuantity.Should().Be(command.CountOuterQuantity);
        result.Value.TotalSales6w.Should().Be(command.TotalSales6w);
        result.Value.Mean.Should().Be(command.Mean);
        result.Value.LowerLimit.Should().Be(command.LowerLimit);
        result.Value.UpperLimit.Should().Be(command.UpperLimit);
        result.Value.HealthStatus.Should().Be(command.HealthStatus);
        result.Value.StoreRank.Should().Be(command.StoreRank);
        result.Value.AddressLine1.Should().Be(command.AddressLine1);
        result.Value.State.Should().Be(command.State);
        result.Value.County.Should().Be(command.County);

        added.Should().NotBeNull();
        added!.OutletIdentifier.Should().Be(command.OutletIdentifier);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldReturnFailure()
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
            UserId = "tester"
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }
}