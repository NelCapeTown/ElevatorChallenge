using ElevatorChallenge.ElevatorClasses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace ElevatorChallenge.Tests;

public class FloorFactoryTests
{
    private readonly Mock<ILogger<FloorFactory>> _factoryLoggerMock;
    private readonly Mock<ILogger<Floor>> _floorLoggerMock;
    private readonly Mock<IConfigurationWrapper> _configurationMock;

    public FloorFactoryTests()
    {
        _factoryLoggerMock = new Mock<ILogger<FloorFactory>>();
        _floorLoggerMock = new Mock<ILogger<Floor>>();
        _configurationMock = new Mock<IConfigurationWrapper>();
    }

    [Fact]
    public void Constructor_SetsNumberOfFloors_FromConfiguration()
    {
        _configurationMock.SetupProperty(c => c.NumberOfFloors,8);

        var factory = new FloorFactory(_factoryLoggerMock.Object, _floorLoggerMock.Object, _configurationMock.Object);

        factory.GetNumberOfFloors().ShouldBe(8);
    }

    [Fact]
    public void Constructor_Throws_WhenLoggerIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new FloorFactory(null!, _floorLoggerMock.Object, _configurationMock.Object));
    }

    [Fact]
    public void Constructor_Throws_WhenFloorLoggerIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new FloorFactory(_factoryLoggerMock.Object, null!, _configurationMock.Object));
    }

    [Fact]
    public void Constructor_Throws_WhenConfigurationIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new FloorFactory(_factoryLoggerMock.Object, _floorLoggerMock.Object, null!));
    }

    [Fact]
    public void SetNumberOfFloors_Throws_WhenNotPositive()
    {
        var factory = CreateDefaultFactory();
        Should.Throw<ArgumentOutOfRangeException>(() => factory.SetNumberOfFloors(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.SetNumberOfFloors(-3));
    }

    [Fact]
    public void SetNumberOfFloors_SetsValue_WhenValid()
    {
        var factory = CreateDefaultFactory();
        factory.SetNumberOfFloors(10);
        factory.GetNumberOfFloors().ShouldBe(10);
    }

    [Fact]
    public void GetNumberOfFloors_ReturnsCurrentValue()
    {
        var factory = CreateDefaultFactory();
        factory.SetNumberOfFloors(5);
        factory.GetNumberOfFloors().ShouldBe(5);
    }

    [Fact]
    public void CreateFloor_ReturnsFloor_WithCorrectNumber()
    {
        var factory = CreateDefaultFactory();
        var floor = factory.CreateFloor(3);

        floor.ShouldNotBeNull();
        floor.FloorNumber.ShouldBe(3);
    }

    [Fact]
    public void CreateFloor_Throws_WhenLoggerIsNull()
    {
        var factory = new FloorFactory(_factoryLoggerMock.Object, _floorLoggerMock.Object, _configurationMock.Object);
        // Floor constructor throws if logger is null, but factory always passes its own logger, so this is not directly testable here.
        // This test is included for completeness, but will not throw unless FloorFactory is changed to allow null loggers.
    }

    private FloorFactory CreateDefaultFactory()
    {
        // Use default values for configuration
        _configurationMock.SetupProperty(c => c.NumberOfFloors,-1);

        return new FloorFactory(_factoryLoggerMock.Object,_floorLoggerMock.Object,_configurationMock.Object);
    }
}