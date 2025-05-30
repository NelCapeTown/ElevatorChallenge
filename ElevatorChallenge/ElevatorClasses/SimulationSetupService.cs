using ElevatorChallenge.Enums;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

public interface ISimulationSetupService
{
    /// <summary>
    /// Sets up the building and starts the main simulation loop.
    /// </summary>
    void GetBuildingUpAndRunning();
}

/// <summary>
/// Handles the setup and initialization of the building simulation, including user prompts and the main simulation loop.
/// </summary>
public class SimulationSetupService : ISimulationSetupService
{
    private readonly IConfiguration _configuration;
    private readonly IFloorFactory _floorFactory;
    private readonly IElevatorFactory _elevatorFactory;
    private readonly IBuilding _building;
    private readonly ILogger<SimulationSetupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSetupService"/> class.
    /// </summary>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="floorFactory">The factory for creating floors.</param>
    /// <param name="elevatorFactory">The factory for creating elevators.</param>
    /// <param name="building">The building instance to initialize and run.</param>
    /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
    public SimulationSetupService(
        ILogger<SimulationSetupService> logger,
        IConfiguration configuration,
        IFloorFactory floorFactory,
        IElevatorFactory elevatorFactory,
        IBuilding building
        )
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _floorFactory = floorFactory ?? throw new ArgumentNullException(nameof(floorFactory));
        _elevatorFactory = elevatorFactory ?? throw new ArgumentNullException(nameof(elevatorFactory));
        _building = building ?? throw new ArgumentNullException(nameof(building));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets up the building, prompts the user for configuration if needed, and starts the simulation loop.
    /// </summary>
    public void GetBuildingUpAndRunning()
    {
        _logger.LogInformation("Starting building setup process...");

        int numFloors = _floorFactory.GetNumberOfFloors();
        while (numFloors < 1)
        {
            _logger.LogWarning("Number of floors is not set or invalid. Prompting user for input.");
            numFloors = PromptForPositiveInt("Enter number of floors: ");
            _floorFactory.SetNumberOfFloors(numFloors);
        }

        int numElevators = _elevatorFactory.GetNumberOfElevators();
        while (numElevators < 1)
        {
            _logger.LogWarning("Number of elevators is not set or invalid. Prompting user for input.");
            numElevators = PromptForPositiveInt("Enter number of elevators: ");
            _elevatorFactory.SetNoOfElevators(numElevators);
        }

        int elevatorCapacity = _elevatorFactory.GetElevatorMaxCapacity();
        while (elevatorCapacity < 1)
        {
            _logger.LogWarning("Elevator capacity is not set or invalid. Prompting user for input.");
            elevatorCapacity = PromptForPositiveInt("Enter elevator capacity: ");
            _elevatorFactory.SetElevatorMaxCapacity(elevatorCapacity);
        }

        _logger.LogInformation("Building parameters: Floors={numFloors}, Elevators={numElevators}, Capacity={Capacity}",numFloors,numElevators,elevatorCapacity);

        var floors = new List<IFloor>();
        for (int i = 1; i <= numFloors; i++)
        {
            floors.Add(_floorFactory.CreateFloor(i));
        }
        _logger.LogDebug("Created {FloorCount} floors.",floors.Count);

        var elevators = new List<IElevator>();
        for (int i = 1; i <= numElevators; i++)
        {
            // Assuming all elevators start at floor 1
            elevators.Add(_elevatorFactory.CreateElevator());
        }
        _logger.LogDebug("Created {numElevators} elevators.",elevators.Count);

        _building.Initialise(elevators,floors);

        _logger.LogInformation("Building setup complete. Ready for simulation.");
        RunBuildingSimulation();
    }

    /// <summary>
    /// Prompts the user to enter a positive integer, optionally accepting a default value.
    /// </summary>
    /// <param name="message">The prompt message to display to the user.</param>
    /// <param name="defaultValue">The default value to use if the user enters nothing.</param>
    /// <returns>A positive integer entered by the user or the default value.</returns>
    private int PromptForPositiveInt(string message,bool allowZero = false,int? defaultValue = null)
    {
        int minValue = allowZero ? 0 : 1;
        while (true)
        {
            Console.Write($"{message} {(defaultValue.HasValue ? $"[{defaultValue}]" : "")}: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) && defaultValue.HasValue)
            {
                _logger.LogDebug("User accepted default value {DefaultValue} for '{Prompt}'",defaultValue.Value,message);
                return defaultValue.Value;
            }

            if (int.TryParse(input,out int value) && value >= minValue)
            {
                _logger.LogDebug("User entered value {Value} for '{Prompt}'",value,message);
                return value;
            }

            _logger.LogWarning($"Invalid input '{input}' for '{message}'. Please enter a valid integer greater thanb or equal to {minValue}.");
            Console.WriteLine($"Invalid input '{input}' for '{message}'. Please enter a valid integer greater thanb or equal to {minValue}.");
        }
    }

    /// <summary>
    /// Runs the main building simulation loop, processing user commands and updating the simulation state.
    /// </summary>
    private void RunBuildingSimulation()
    {
        _logger.LogInformation("Starting building simulation...");

        // --- Simulation Loop ---
        bool running = true;
        while (running)
        {
            Console.WriteLine("Please enter one of:");
            Console.WriteLine("+ (c)all <FloorNumber> <up/down/u/d> [NumberOfPeople] : Simulates pressing up or down button on a floor.");
            Console.WriteLine("+ (s)tep : Advance one step");
            Console.WriteLine("+ (d)isplay : Display building status without advancing a step.");
            Console.WriteLine("+ (e)xit : Exit the application.");
            Console.Write("> ");
            string? rawInput = Console.ReadLine();
            string S_input = rawInput?.ToLower().Trim() ?? string.Empty; // Use S_input for safety
            string[] parts = S_input.Split(' ',StringSplitOptions.RemoveEmptyEntries); // Remove empty entries for robustness

            if (parts.Length == 0) // Handle empty input
            {
                Console.WriteLine("No command entered.");
                continue;
            }

            string command = parts[0];

            try
            {
                switch (command)
                {
                    case "call":
                    case "c":
                        if (parts.Length >= 3)
                        {
                            int callFloor;
                            string? floorInput = parts[1];
                            while (!int.TryParse(floorInput,out callFloor) || callFloor < 1) // Assuming floors are 1-indexed
                            {
                                Console.WriteLine();
                                callFloor = PromptForPositiveInt("Invalid floor number. Please enter a positive integer for the floor: ",false,1);
                            }

                            Direction callDirection;
                            string directionInput = parts[2].ToLower();
                            switch (directionInput)
                            {
                                case "up":
                                case "u":
                                    callDirection = Direction.Up;
                                    break;
                                case "down":
                                case "d":
                                    callDirection = Direction.Down;
                                    break;
                                default:
                                    throw new ArgumentException("Invalid direction. Use 'up', 'u', 'down', or 'd'.");
                            }

                            int numberOfPeople = 1; // Default to 1 person
                            if (parts.Length > 3)
                            {
                                string? peopleInput = parts[3];
                                while (!int.TryParse(peopleInput,out numberOfPeople) || numberOfPeople < 1)
                                {
                                    Console.WriteLine("Invalid number of people. Please enter a positive integer:");
                                    numberOfPeople = PromptForPositiveInt("Please enter number of people: ",true,1);
                                }
                            }

                            // Assuming _building is properly initialized
                            _building.RequestElevator(callFloor,callDirection,numberOfPeople);
                            _logger.LogInformation("Elevator called to floor {Floor} for {Direction} for {People} people.",callFloor,callDirection,numberOfPeople);
                        }
                        else
                        {
                            Console.WriteLine("Usage: call <floorNumber> <direction> [numberOfPeople]");
                        }
                        break;

                    case "step":
                    case "s":
                        // Assuming _building is properly initialized
                        _building.StepSimulation();
                        _building.DisplayStatus();
                        _logger.LogInformation("Simulation step executed.");
                        // Thread.Sleep(500); // Optional: slow down simulation for viewing
                        break;

                    case "display":
                    case "d":
                        // Assuming _building is properly initialized
                        _building.DisplayStatus();
                        break;

                    case "exit":
                    case "e":
                        running = false;
                        _logger.LogInformation("Exiting simulation by user command.");
                        Console.WriteLine("Exiting simulation.");
                        break;

                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error processing command '{UserCommand}'.",S_input);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
