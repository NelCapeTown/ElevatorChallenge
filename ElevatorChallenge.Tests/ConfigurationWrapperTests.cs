using System.Collections.Generic;
using ElevatorChallenge.ElevatorClasses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElevatorChallenge.Tests;

public class ConfigurationWrapperTests
{
    [Fact]
    public void ParameterlessConstructor_AllProperties_DefaultValues()
    {
        var config = new ConfigurationWrapper();

        Assert.Equal(-1, config.MaxElevatorCapacity);
        Assert.Equal(-1, config.DefaultElevatorStartingFloor);
        Assert.Equal(-1, config.NumberOfElevators);
        Assert.Equal(-1, config.NumberOfFloors);
    }

    [Fact]
    public void ParameterlessConstructor_SetAndGetProperties()
    {
        var config = new ConfigurationWrapper
        {
            MaxElevatorCapacity = 10,
            DefaultElevatorStartingFloor = 2,
            NumberOfElevators = 3,
            NumberOfFloors = 15
        };

        Assert.Equal(10, config.MaxElevatorCapacity);
        Assert.Equal(2, config.DefaultElevatorStartingFloor);
        Assert.Equal(3, config.NumberOfElevators);
        Assert.Equal(15, config.NumberOfFloors);
    }

    [Fact]
    public void ProductionConstructor_ReadsValuesFromIConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "MaxElevatorCapacity", "8" },
            { "DefaultElevatorStartingFloor", "1" },
            { "NumberOfElevators", "4" },
            { "NumberOfFloors", "20" }
        };

        // Convert Dictionary<string, string> to IEnumerable<KeyValuePair<string, string?>>
        var inMemorySettingsNullable = inMemorySettings
            .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettingsNullable) // Use the converted collection
            .Build();

        var loggerMock = new Mock<ILogger<ConfigurationWrapper>>();
        var config = new ConfigurationWrapper(loggerMock.Object, configuration);

        Assert.Equal(8, config.MaxElevatorCapacity);
        Assert.Equal(1, config.DefaultElevatorStartingFloor);
        Assert.Equal(4, config.NumberOfElevators);
        Assert.Equal(20, config.NumberOfFloors);
    }

    [Fact]
    public void ProductionConstructor_UsesDefaults_WhenKeysMissing()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var loggerMock = new Mock<ILogger<ConfigurationWrapper>>();
        var config = new ConfigurationWrapper(loggerMock.Object, configuration);

        Assert.Equal(-1, config.MaxElevatorCapacity);
        Assert.Equal(-1, config.DefaultElevatorStartingFloor);
        Assert.Equal(-1, config.NumberOfElevators);
        Assert.Equal(-1, config.NumberOfFloors);
    }

    [Fact]
    public void IConfigurationWrapper_CanBeMockedForTesting()
    {
        var mock = new Mock<IConfigurationWrapper>();
        mock.SetupProperty(c => c.MaxElevatorCapacity, 12);
        mock.SetupProperty(c => c.DefaultElevatorStartingFloor, 5);
        mock.SetupProperty(c => c.NumberOfElevators, 2);
        mock.SetupProperty(c => c.NumberOfFloors, 7);

        Assert.Equal(12, mock.Object.MaxElevatorCapacity);
        Assert.Equal(5, mock.Object.DefaultElevatorStartingFloor);
        Assert.Equal(2, mock.Object.NumberOfElevators);
        Assert.Equal(7, mock.Object.NumberOfFloors);
    }
}