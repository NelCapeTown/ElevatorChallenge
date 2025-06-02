using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Provides configuration values for elevator settings.
/// </summary>
public interface IConfigurationWrapper
{
    /// <summary>
    /// Gets or sets the maximum number of passengers that an elevator can carry.
    /// </summary>
    int MaxElevatorCapacity { get; set; }

    /// <summary>
    /// Gets or sets the default starting floor for elevators.
    /// </summary>
    int DefaultElevatorStartingFloor { get; set; }

    /// <summary>
    /// Gets or sets the number of elevators in the building.
    /// </summary>
    int NumberOfElevators { get; set; }

    /// <summary>
    /// Gets or sets the number of floors in the building.
    /// </summary>
    int NumberOfFloors { get; set; }
}

/// <summary>
/// A simple configuration wrapper that enables easier testing and configuration management for elevator settings.
/// </summary>
public class ConfigurationWrapper : IConfigurationWrapper
{
    private readonly ILogger<ConfigurationWrapper>? _logger;
    private readonly IConfiguration? _configuration;

    /// <summary>
    /// Gets or sets the maximum number of passengers that an elevator can carry.
    /// Defaults to -1 if not set.
    /// </summary>
    public int MaxElevatorCapacity { get; set; } = -1;

    /// <summary>
    /// Gets or sets the default starting floor for elevators.
    /// Defaults to -1 if not set.
    /// </summary>
    public int DefaultElevatorStartingFloor { get; set; } = -1;

    /// <summary>
    /// Gets or sets the number of elevators in the building.
    /// Defaults to -1 if not set.
    /// </summary>
    public int NumberOfElevators { get; set; } = -1;

    /// <summary>
    /// Gets or sets the number of floors in the building.
    /// Defaults to -1 if not set.
    /// </summary>
    public int NumberOfFloors { get; set; } = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationWrapper"/> class for testing scenarios.
    /// </summary>
    public ConfigurationWrapper() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationWrapper"/> class using the specified logger and configuration.
    /// </summary>
    /// <param name="logger">The logger instance for logging configuration events.</param>
    /// <param name="configuration">The configuration instance to retrieve values from.</param>
    public ConfigurationWrapper(ILogger<ConfigurationWrapper> logger, IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        MaxElevatorCapacity = configuration.GetValue<int>("MaxElevatorCapacity", -1);
        DefaultElevatorStartingFloor = configuration.GetValue<int>("DefaultElevatorStartingFloor", -1);
        NumberOfElevators = configuration.GetValue<int>("NumberOfElevators", -1);
        NumberOfFloors = configuration.GetValue<int>("NumberOfFloors", -1);
    }
}
