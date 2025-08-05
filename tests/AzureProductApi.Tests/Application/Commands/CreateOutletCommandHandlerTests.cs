using AutoFixture;
using AutoFixture.Xunit2;
using AutoMapper;
using AzureProductApi.Application.Common.Mappings;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Application.Outlets.Commands.CreateOutlet;
using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.Enums;
using AzureProductApi.Domain.Interfaces;
using AzureProductApi.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureProductApi.Tests.Application.Commands;

public class CreateOutletCommandHandlerTests
{
    private readonly Mock<IOutletRepository> _mockRepository;
    private readonly IMapper _mapper;
    private readonly Mock<ILogger<CreateOutletCommandHandler>> _mockLogger;
    private readonly CreateOutletCommandHandler _handler;
    private readonly IFixture _fixture;

    public CreateOutletCommandHandlerTests()
    {
        _mockRepository = new Mock<IOutletRepository>();
        _mockLogger = new Mock<ILogger<CreateOutletCommandHandler>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        
        _handler = new CreateOutletCommandHandler(_mockRepository.Object, _mapper, _mockLogger.Object);
        
        _fixture = new Fixture();
        _fixture.Customize<Money>(c => c.FromFactory(() => Money.Usd(_fixture.Create<decimal>())));
        _fixture.Customize<Address>(c => c.FromFactory(() => 
            new Address("123 Main St", "New York", "NY", "10001", "USA")));
    }

    [Theory, AutoData]
    public async Task Handle_WithValidCommand_ShouldCreateOutlet(string userId)
    {
        // Arrange
        var command = new CreateOutletCommand
        {
            Name = "Test Outlet",
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
            },
            UserId = userId
        };

        _mockRepository.Setup(r => r.ExistsWithNameAndLocationAsync(
                command.Name, command.Address.City, command.Address.State, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Outlet outlet, CancellationToken _) => outlet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(command.Name);
        result.Value.Tier.Should().Be(command.Tier);
        result.Value.Rank.Should().Be(command.Rank);
        result.Value.ChainType.Should().Be(command.ChainType);
        result.Value.Sales.Should().Be(command.Sales);
        result.Value.Currency.Should().Be(command.Currency);
        result.Value.VolumeSoldKg.Should().Be(command.VolumeSoldKg);
        result.Value.VolumeTargetKg.Should().Be(command.VolumeTargetKg);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task Handle_WithDuplicateOutlet_ShouldReturnFailure(string userId)
    {
        // Arrange
        var command = new CreateOutletCommand
        {
            Name = "Test Outlet",
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
            },
            UserId = userId
        };

        _mockRepository.Setup(r => r.ExistsWithNameAndLocationAsync(
                command.Name, command.Address.City, command.Address.State, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task Handle_WithInvalidMoney_ShouldReturnFailure(string userId)
    {
        // Arrange
        var command = new CreateOutletCommand
        {
            Name = "Test Outlet",
            Tier = "Premium",
            Rank = 1,
            ChainType = ChainType.National,
            Sales = -100m, // Invalid negative amount
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
            },
            UserId = userId
        };

        _mockRepository.Setup(r => r.ExistsWithNameAndLocationAsync(
                command.Name, command.Address.City, command.Address.State, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task Handle_WithRepositoryException_ShouldReturnFailure(string userId)
    {
        // Arrange
        var command = new CreateOutletCommand
        {
            Name = "Test Outlet",
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
            },
            UserId = userId
        };

        _mockRepository.Setup(r => r.ExistsWithNameAndLocationAsync(
                command.Name, command.Address.City, command.Address.State, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("error occurred");
    }

    [Theory, AutoData]
    public async Task Handle_WithLastVisitDate_ShouldSetVisitDate(string userId)
    {
        // Arrange
        var visitDate = DateTime.UtcNow.AddDays(-5);
        var command = new CreateOutletCommand
        {
            Name = "Test Outlet",
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
            },
            LastVisitDate = visitDate,
            UserId = userId
        };

        _mockRepository.Setup(r => r.ExistsWithNameAndLocationAsync(
                command.Name, command.Address.City, command.Address.State, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Outlet outlet, CancellationToken _) => outlet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.LastVisitDate.Should().Be(visitDate);
    }
}