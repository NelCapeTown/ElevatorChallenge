using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Interface for creating <see cref="Floor"/> instances and managing the number of floors in the building.
/// </summary>
public interface IFloorFactory
{
    /// <summary>
    /// Creates a new <see cref="Floor"/> instance with the specified floor number.
    /// </summary>
    /// <param name="floorNumber">The number to assign to the new floor.</param>
    /// <returns>A new <see cref="Floor"/> object.</returns>
    Floor CreateFloor(int floorNumber);

    /// <summary>
    /// Gets the configured number of floors for the building.
    /// </summary>
    /// <returns>The total number of floors.</returns>
    int GetNumberOfFloors();

    /// <summary>
    /// Sets the total number of floors for the building.
    /// </summary>
    /// <param name="numberOfFloors">The number of floors to set. Must be greater than zero.</param>
    void SetNumberOfFloors(int numberOfFloors);
}

/// <summary>
/// Factory for creating <see cref="Floor"/> instances and managing the number of floors in the building.
/// </summary>
public class FloorFactory : IFloorFactory
{
    private readonly ILogger<FloorFactory> _logger;
    private readonly ILogger<Floor> _floorLogger;
    private readonly IConfigurationWrapper _configurationWrapper;

    private int _numberOfFloors;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for the <see cref="FloorFactory"/> instance.</param>
    /// <param name="floorLogger">The logger for the <see cref="Floor"/> instances.</param>
    /// <param name="configurationWrapper">The configurationWrapper settings for the application.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="logger"/>, <paramref name="floorLogger"/>, or <paramref name="configurationWrapper"/> is null.
    /// </exception>
    public FloorFactory(ILogger<FloorFactory> logger, ILogger<Floor> floorLogger, IConfigurationWrapper configurationWrapper)
    {

        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

        _floorLogger = floorLogger ?? throw new ArgumentNullException(nameof(floorLogger), "Floor logger cannot be null.");

        _configurationWrapper = configurationWrapper ?? throw new ArgumentNullException(nameof(configurationWrapper), "Configuration cannot be null.");

        _numberOfFloors = _configurationWrapper.NumberOfFloors; // Default to -1 if not specified in which case the user will be prompted to enter a value

    }

    /// <summary>
    /// Sets the total number of floors for the building.
    /// </summary>
    /// <param name="numberOfFloors">The number of floors to set. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="numberOfFloors"/> is less than or equal to zero.</exception>
    public void SetNumberOfFloors(int numberOfFloors)
    {
        if (numberOfFloors < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfFloors),"Number of Floors must be one or greater.");
        }
        _numberOfFloors = numberOfFloors;
        _logger.LogInformation($"Number of Floors set to {_numberOfFloors}");
    }
    /// <summary>
    /// Gets the configured number of floors for the building.
    /// </summary>
    /// <returns>The total number of floors.</returns>
    public int GetNumberOfFloors()
    {
        return _numberOfFloors;
    }

    /// <summary>
    /// Creates a new <see cref="Floor"/> instance with the specified floor number.
    /// </summary>
    /// <param name="floorNumber">The number to assign to the new floor.</param>
    /// <returns>A new <see cref="Floor"/> object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="floorNumber"/> is less than zero or greater than or equal to the configured number of floors.
    /// </exception>
    public Floor CreateFloor(int floorNumber)
    {
        try
        {
            _logger.LogInformation($"Creating floor {floorNumber}");
            return new Floor(_floorLogger,floorNumber);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,$"Error in CreateFloor: {floorNumber}");
            throw;
        }
    }
}
