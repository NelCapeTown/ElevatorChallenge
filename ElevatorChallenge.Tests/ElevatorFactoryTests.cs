using ElevatorChallenge.ElevatorClasses;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using Xunit;

namespace ElevatorChallenge.Tests;

public class ElevatorFactoryTests
{
    private readonly Mock<ILogger<ElevatorFactory>> _factoryLoggerMock;
    private readonly Mock<ILogger<Elevator>> _elevatorLoggerMock;
    private readonly Mock<IConfigurationWrapper> _configurationMock;

    public ElevatorFactoryTests()
    {
        _factoryLoggerMock = new Mock<ILogger<ElevatorFactory>>();
        _elevatorLoggerMock = new Mock<ILogger<Elevator>>();
        _configurationMock = new Mock<IConfigurationWrapper>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ElevatorFactory(null!,_elevatorLoggerMock.Object,_configurationMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenElevatorLoggerIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ElevatorFactory(_factoryLoggerMock.Object,null!,_configurationMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ElevatorFactory(_factoryLoggerMock.Object,_elevatorLoggerMock.Object,null!));
    }

    [Fact]
    public void Constructor_SetsFields_FromConfiguration()
    {
        _configurationMock.SetupProperty(c => c.MaxElevatorCapacity, 8);
        _configurationMock.SetupProperty(c => c.DefaultElevatorStartingFloor, 2);
        _configurationMock.SetupProperty(c => c.NumberOfElevators, 3);

        var factory = new ElevatorFactory(_factoryLoggerMock.Object,_elevatorLoggerMock.Object,_configurationMock.Object);

        factory.GetElevatorMaxCapacity().ShouldBe(8);
        factory.GetDefaultStartingFloor().ShouldBe(2);
        factory.GetNumberOfElevators().ShouldBe(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SetElevatorMaxCapacity_Throws_WhenCapacityIsNotPositive(int invalidCapacity)
    {
        var factory = CreateDefaultFactory();
        Should.Throw<ArgumentOutOfRangeException>(() => factory.SetElevatorMaxCapacity(invalidCapacity));
    }

    [Fact]
    public void SetElevatorMaxCapacity_SetsValue_WhenValid()
    {
        var factory = CreateDefaultFactory();
        factory.SetElevatorMaxCapacity(10);
        factory.GetElevatorMaxCapacity().ShouldBe(10);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SetDefaultStartingFloor_Throws_WhenNegative(int invalidFloor)
    {
        var factory = CreateDefaultFactory();
        Should.Throw<ArgumentOutOfRangeException>(() => factory.SetDefaultStartingFloor(invalidFloor));
    }

    [Fact]
    public void SetDefaultStartingFloor_SetsValue_WhenValid()
    {
        var factory = CreateDefaultFactory();
        factory.SetDefaultStartingFloor(0);
        factory.GetDefaultStartingFloor().ShouldBe(0);
        factory.SetDefaultStartingFloor(5);
        factory.GetDefaultStartingFloor().ShouldBe(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void SetNumberOfElevators_Throws_WhenLessThanOne(int invalidCount)
    {
        var factory = CreateDefaultFactory();
        Should.Throw<ArgumentOutOfRangeException>(() => factory.SetNumberOfElevators(invalidCount));
    }

    [Fact]
    public void SetNumberOfElevators_SetsValue_WhenValid()
    {
        var factory = CreateDefaultFactory();
        factory.SetNumberOfElevators(2);
        factory.GetNumberOfElevators().ShouldBe(2);
    }

    [Fact]
    public void CreateElevator_UsesDefaultStartingFloor_WhenStartingFloorIsNegative()
    {
        var factory = CreateDefaultFactory();
        factory.SetDefaultStartingFloor(3);
        factory.SetElevatorMaxCapacity(7);

        var elevator = factory.CreateElevator(-1);

        elevator.CurrentFloor.ShouldBe(3);
        elevator.MaxCapacity.ShouldBe(7);
    }

    [Fact]
    public void CreateElevator_Throws_WhenDefaultStartingFloorNotSet_And_ParameterNegative()
    {
        var factory = CreateDefaultFactory();
        factory.SetElevatorMaxCapacity(5);
        // Default starting floor is not yet set so it defaults to -1
        Should.Throw<InvalidOperationException>(() => factory.CreateElevator(-1));
    }

    [Fact]
    public void CreateElevator_UsesProvidedStartingFloor_WhenNonNegative()
    {
        var factory = CreateDefaultFactory();
        factory.SetElevatorMaxCapacity(4);

        var elevator = factory.CreateElevator(2);

        elevator.CurrentFloor.ShouldBe(2);
        elevator.MaxCapacity.ShouldBe(4);
    }


    private ElevatorFactory CreateDefaultFactory()
    {
        // Use default values for configuration
        _configurationMock.SetupProperty(c => c.MaxElevatorCapacity, -1);
        _configurationMock.SetupProperty(c => c.DefaultElevatorStartingFloor, -1);
        _configurationMock.SetupProperty(c => c.NumberOfElevators, -1);

        return new ElevatorFactory(_factoryLoggerMock.Object,_elevatorLoggerMock.Object,_configurationMock.Object);
    }
}