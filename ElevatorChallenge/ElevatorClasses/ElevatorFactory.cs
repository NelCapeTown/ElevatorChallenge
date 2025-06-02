using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Interface for creating and managing elevator instances and configurations.
/// </summary>
public interface IElevatorFactory
{
    /// <summary>
    /// Creates a new <see cref="Elevator"/> instance with the specified starting floor.
    /// </summary>
    /// <param name="startingFloor">The floor where the elevator will be initially positioned. If not specified or negative, the default starting floor is used.</param>
    /// <returns>A new <see cref="Elevator"/> object.</returns>
    Elevator CreateElevator(int startingFloor = -1);

    /// <summary>
    /// Gets the default starting floor for new elevators.
    /// </summary>
    /// <returns>The default starting floor.</returns>
    int GetDefaultStartingFloor();

    /// <summary>
    /// Gets the maximum capacity for elevators.
    /// </summary>
    /// <returns>The maximum number of people an elevator can carry.</returns>
    int GetElevatorMaxCapacity();

    /// <summary>
    /// Gets the configured number of elevators.
    /// </summary>
    /// <returns>The number of elevators.</returns>
    int GetNumberOfElevators();

    /// <summary>
    /// Sets the default starting floor for new elevators.
    /// </summary>
    /// <param name="floor">The floor to set as the default starting floor. Must be zero or greater.</param>
    void SetDefaultStartingFloor(int floor);

    /// <summary>
    /// Sets the maximum capacity for elevators.
    /// </summary>
    /// <param name="capacity">The maximum number of people an elevator can carry. Must be greater than zero.</param>
    void SetElevatorMaxCapacity(int capacity);

    /// <summary>
    /// Sets the number of elevators for the building.
    /// </summary>
    /// <param name="numberOfElevators">The number of elevators. Must be one or greater.</param>
    void SetNumberOfElevators(int numberOfElevators);
}

/// <summary>
/// Factory for creating <see cref="Elevator"/> instances and managing elevator configuration.
/// </summary>
public class ElevatorFactory : IElevatorFactory
{
    private readonly ILogger<ElevatorFactory> _logger;
    private readonly ILogger<Elevator> _elevatorLogger;
    private readonly IConfigurationWrapper _configurationWrapper;
    private int _elevatorMaxCapacity;
    private int _defaultStartingFloor;
    private int _numberOfElevators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElevatorFactory"/> class, configuring elevator settings and logging
    /// dependencies.
    /// </summary>
    /// <remarks>The constructor retrieves the following settings from the provided <paramref
    /// name="configurationWrapper"/>: <list type="bullet"> <item> <description><c>MaxElevatorCapacity</c>: The maximum
    /// capacity of an elevator. Defaults to -1 if not specified, prompting the user to provide a value.</description>
    /// </item> <item> <description><c>DefaultElevatorStartingFloor</c>: The default starting floor for elevators.
    /// Defaults to -1 if not specified, prompting the user to provide a value.</description> </item> <item>
    /// <description><c>NumberOfElevators</c>: The number of elevators to create. Defaults to -1 if not specified,
    /// prompting the user to provide a value.</description> </item> </list></remarks>
    /// <param name="logger">The logger used for logging factory-level operations. Cannot be <see langword="null"/>.</param>
    /// <param name="elevatorLogger">The logger used for logging elevator-specific operations. Cannot be <see langword="null"/>.</param>
    /// <param name="configurationWrapper">The configuration source for retrieving elevator settings. Cannot be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="elevatorLogger"/>, or <paramref name="configurationWrapper"/> is
    /// <see langword="null"/>.</exception>
    public ElevatorFactory(ILogger<ElevatorFactory> logger,ILogger<Elevator> elevatorLogger,IConfigurationWrapper configurationWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger),"Logger cannot be null.");
        _elevatorLogger = elevatorLogger ?? throw new ArgumentNullException(nameof(elevatorLogger),"Elevator logger cannot be null.");
        _configurationWrapper = configurationWrapper ?? throw new ArgumentNullException(nameof(configurationWrapper),"Configuration cannot be null.");
        _elevatorMaxCapacity = _configurationWrapper.MaxElevatorCapacity; // Default to -1 if not specified in which case the user will be prompted to enter a value
        _defaultStartingFloor = _configurationWrapper.DefaultElevatorStartingFloor; // Default to -1 if not specified in which case the user will be prompted to enter a value
        _numberOfElevators = _configurationWrapper.NumberOfElevators; // Default to -1 if not specified in which case the user will be prompted to enter a value
    }

    /// <summary>
    /// Sets the maximum capacity for elevators.
    /// </summary>
    /// <param name="capacity">The maximum number of people an elevator can carry. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is less than or equal to zero.</exception>
    public void SetElevatorMaxCapacity(int capacity)
    {
        try
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity),"Elevator capacity must be greater than zero.");
            }
            _elevatorMaxCapacity = capacity;
            _logger.LogInformation($"Elevator max capacity set to {_elevatorMaxCapacity}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error in SetElevatorMaxCapacity.");
            throw;
        }
    }

    /// <summary>
    /// Sets the default starting floor for new elevators.
    /// </summary>
    /// <param name="floor">The floor to set as the default starting floor. Must be zero or greater.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="floor"/> is less than zero.</exception>
    public void SetDefaultStartingFloor(int floor)
    {
        if (floor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floor),"Default starting floor must be zero or greater.");
        }
        _defaultStartingFloor = floor;
        _logger.LogInformation($"Default starting floor set to {_defaultStartingFloor}");
    }

    /// <summary>
    /// Sets the number of elevators for the building.
    /// </summary>
    /// <param name="numberOfElevators">The number of elevators. Must be one or greater.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="numberOfElevators"/> is less than one.</exception>
    public void SetNumberOfElevators(int numberOfElevators)
    {
        if (numberOfElevators < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfElevators),"Number of Elevators must be one or greater.");
        }
        _numberOfElevators = numberOfElevators;
        _logger.LogInformation($"Number of Elevators set to {_numberOfElevators}");
    }

    /// <summary>
    /// Gets the maximum capacity for elevators.
    /// </summary>
    /// <returns>The maximum number of people an elevator can carry.</returns>
    public int GetElevatorMaxCapacity()
    {
        return _elevatorMaxCapacity;
    }

    /// <summary>
    /// Gets the default starting floor for new elevators.
    /// </summary>
    /// <returns>The default starting floor.</returns>
    public int GetDefaultStartingFloor()
    {
        return _defaultStartingFloor;
    }

    /// <summary>
    /// Gets the configured number of elevators.
    /// </summary>
    /// <returns>The number of elevators.</returns>
    public int GetNumberOfElevators()
    {
        return _numberOfElevators;
    }

    /// <summary>
    /// Creates a new <see cref="Elevator"/> instance with the specified starting floor.
    /// </summary>
    /// <param name="startingFloor">The floor where the elevator will be initially positioned. If not specified or negative, the default starting floor is used.</param>
    /// <returns>A new <see cref="Elevator"/> object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the default starting floor is not set and <paramref name="startingFloor"/> is negative.</exception>
    public Elevator CreateElevator(int startingFloor = -1)
    {
        if (startingFloor < 0)
        {
            startingFloor = _defaultStartingFloor;
            if (startingFloor < 0)
            {
                throw new InvalidOperationException("Default starting floor is not set. Please set it before creating an elevator.");
            }
        }
        var elevator = new Elevator(_elevatorLogger,startingFloor,_elevatorMaxCapacity);
        _logger.LogInformation($"Elevator created at floor {startingFloor} with capacity {_elevatorMaxCapacity}");
        return elevator;
    }
}
