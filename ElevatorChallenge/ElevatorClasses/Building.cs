using ElevatorChallenge.Enums;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Represents the contract for a building that manages elevators and floors.
/// </summary>
public interface IBuilding
{
    /// <summary>
    /// Displays the current status of all elevators and floors in the building.
    /// </summary>
    void DisplayStatus();

    /// <summary>
    /// Initializes the building with the specified elevators and floors.
    /// </summary>
    /// <param name="elevators">The collection of elevators to be managed by the building.</param>
    /// <param name="floors">The collection of floors in the building.</param>
    void Initialise(IEnumerable<IElevator> elevators,IEnumerable<IFloor> floors);

    /// <summary>
    /// Handles a call request from a floor and assigns the nearest available elevator.
    /// </summary>
    /// <param name="floorNumber">The floor number where the call originates.</param>
    /// <param name="direction">The direction in which the people want to travel.</param>
    /// <param name="numberOfPeople">The number of people making the request. Defaults to 1 if not specified.</param>
    void RequestElevator(int floorNumber,Direction direction,int numberOfPeople = 1);

    /// <summary>
    /// Advances the simulation by one time step.
    /// </summary>
    void StepSimulation();
}

/// <summary>
/// Manages all elevators and floors, and handles call requests.
/// This is the "brains" of the simulation.
/// </summary>
public class Building : IBuilding
{
    private readonly ILogger<IBuilding> _logger;
    private IEnumerable<IElevator>? _elevators;
    private IEnumerable<IFloor>? _floors;
    private int _totalFloors;
    private readonly Random _random = new Random();

    /// <summary>
    /// Initializes a new instance of the <see cref="Building"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for building operations.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is null.</exception>
    public Building(ILogger<IBuilding> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger),"Logger cannot be null.");
        _elevators = Enumerable.Empty<IElevator>();
        _floors = Enumerable.Empty<IFloor>();
        _totalFloors = 0;
        _logger.LogDebug($"Building instance created. Total Floors: {_totalFloors}");
    }

    /// <summary>
    /// Initializes the building with the specified elevators and floors.
    /// </summary>
    /// <param name="elevators">The collection of elevators to be managed by the building.</param>
    /// <param name="floors">The collection of floors in the building.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="elevators"/> or <paramref name="floors"/> is null.</exception>
    public void Initialise(IEnumerable<IElevator> elevators,IEnumerable<IFloor> floors)
    {
        try
        {
            _elevators = elevators ?? throw new ArgumentNullException(nameof(elevators),"Elevators cannot be null.");
            _floors = floors ?? throw new ArgumentNullException(nameof(floors),"Floors cannot be null.");
            _totalFloors = _floors.Count();
            _logger.LogInformation($"Building initialised with {_elevators.Count()} elevators and {_floors.Count()} floors. Operational floors: {_totalFloors}.");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error Initialising Building.");
            throw;
        }
    }

    /// <summary>
    /// Handles a call request from a floor and assigns the nearest available elevator.
    /// Creates the specified number of people waiting on the floor and attempts to dispatch an elevator.
    /// </summary>
    public void RequestElevator(int floorNumber,Direction direction,int numberOfPeople = 1)
    {
        if (_totalFloors == 0)
        {
            _logger.LogWarning("RequestElevator: Building not properly initialized (totalFloors is 0).");
            return;
        }
        if (floorNumber < 1 || floorNumber > _totalFloors)
        {
            _logger.LogWarning($"RequestElevator: Invalid floor number {floorNumber}. Building has {_totalFloors} floors.");
            return;
        }

        if (!_floors?.Any() ?? true)
        {
            _logger.LogError("RequestElevator: No floors available in the building.");
            return;
        }

        IFloor? callingFloor = _floors!.FirstOrDefault(f => f.FloorNumber == floorNumber);
        if (callingFloor == null)
        {
            _logger.LogWarning($"RequestElevator: Requested floor {floorNumber} does not exist.");
            return;
        }

        int peopleSuccessfullyCreated = 0;
        for (int i = 0; i < numberOfPeople; i++)
        {
            int destinationFloor;
            if (direction == Direction.Up)
            {
                if (floorNumber >= _totalFloors)
                {
                    _logger.LogDebug($"RequestElevator: Cannot go UP from top floor {floorNumber} for person {i + 1}.");
                    continue; // Skip creating this person
                }
                destinationFloor = _random.Next(floorNumber + 1,_totalFloors + 1);
            }
            else // Direction.Down
            {
                if (floorNumber <= 1)
                {
                    _logger.LogDebug($"RequestElevator: Cannot go DOWN from bottom floor {floorNumber} for person {i + 1}.");
                    continue; // Skip creating this person
                }
                destinationFloor = _random.Next(1,floorNumber);
            }
            if (destinationFloor == floorNumber && _totalFloors > 1)
            {
                _logger.LogWarning($"RequestElevator: Generated same destination floor {destinationFloor} for origin {floorNumber}. This should be rare.");
            }

            Person p = new Person(floorNumber,destinationFloor);
            callingFloor.AddWaitingPerson(p);
            peopleSuccessfullyCreated++;
        }

        if (peopleSuccessfullyCreated > 0)
        {
            _logger.LogInformation($"RequestElevator: Call from floor {floorNumber} ({direction}) for {peopleSuccessfullyCreated} people (requested {numberOfPeople}).");
        }
        else if (numberOfPeople > 0)
        {
            _logger.LogInformation($"RequestElevator: Call from floor {floorNumber} ({direction}) for {numberOfPeople} people, but no valid journeys could be created (e.g. trying to go up from top floor).");
        }

        if (!_elevators?.Any() ?? true)
        {
            _logger.LogWarning("RequestElevator: No elevators available in the building to dispatch.");
            return;
        }

        IElevator? bestElevator = FindBestElevatorForCall(floorNumber,direction);
        if (bestElevator != null)
        {
            _logger.LogInformation($"RequestElevator: Assigning Elevator E{bestElevator.Id} to floor {floorNumber} ({direction}).");
            bestElevator.AddDestination(floorNumber,direction);
        }
        else
        {
            _logger.LogInformation($"RequestElevator: No suitable elevator found for floor {floorNumber} ({direction}). Request queued by people waiting.");
        }
    }

    /// <summary>
    /// Finds the best elevator to service a call from a given floor and direction.
    /// </summary>
    private IElevator? FindBestElevatorForCall(int callFloor,Direction callDirection)
    {
        if (_elevators == null || !_elevators.Any()) return null;

        IElevator? bestChoice = null;
        int minScore = int.MaxValue;

        foreach (var elevator in _elevators)
        {
            if (elevator.Passengers.Count >= elevator.MaxCapacity &&
                !elevator.Passengers.Any(p => p.DestinationFloor == callFloor))
            {
                continue;
            }

            int score;
            int distanceToCallFloor = Math.Abs(elevator.CurrentFloor - callFloor);

            if (elevator.IsIdle())
            {
                score = distanceToCallFloor;
            }
            else if (elevator.CurrentDirection == callDirection &&
                     ((callDirection == Direction.Up && elevator.CurrentFloor <= callFloor) ||
                      (callDirection == Direction.Down && elevator.CurrentFloor >= callFloor)))
            {
                score = distanceToCallFloor;
            }
            else
            {
                score = distanceToCallFloor + _totalFloors;
            }

            score += elevator.Passengers.Count;
            score += elevator.AllUpcomingStops.Count;

            if (score < minScore)
            {
                minScore = score;
                bestChoice = elevator;
            }
            else if (score == minScore && bestChoice != null)
            {
                if (elevator.Passengers.Count < bestChoice.Passengers.Count)
                {
                    bestChoice = elevator;
                }
                else if (elevator.Passengers.Count == bestChoice.Passengers.Count && elevator.Id < bestChoice.Id)
                {
                    bestChoice = elevator;
                }
            }
        }
        if (bestChoice != null)
            _logger.LogDebug($"FindBestElevatorForCall: Best choice for F{callFloor}({callDirection}) is E{bestChoice.Id} with score {minScore}.");
        else
            _logger.LogDebug($"FindBestElevatorForCall: No suitable elevator found for F{callFloor}({callDirection}).");

        return bestChoice;
    }


    /// <summary>
    /// Advances the simulation by one time step.
    /// Each elevator performs its actions, and waiting passengers are loaded if possible.
    /// </summary>
    public void StepSimulation()
    {
        if (_elevators == null || !_elevators.Any())
        {
            _logger.LogWarning("StepSimulation: No elevators in the building to simulate.");
            return;
        }
        if (_floors == null || !_floors.Any())
        {
            _logger.LogWarning("StepSimulation: No floors in the building. Passenger loading cannot occur.");
        }
        if (_totalFloors == 0)
        {
            _logger.LogWarning("StepSimulation: Building total floors not set. Simulation might be unreliable.");
        }

        _logger.LogInformation("--- Building Step Simulation Tick ---");
        Console.WriteLine("--- Building Step Simulation Tick ---");

        _logger.LogDebug("Phase 1: Updating all elevator states and actions.");
        foreach (IElevator elevator in _elevators)
        {
            _logger.LogTrace($"Stepping Elevator{elevator.Id} (Currently at Floor {elevator.CurrentFloor}, State: {elevator.State}, Dir: {elevator.CurrentDirection})");
            Console.WriteLine($"Stepping Elevator{elevator.Id} (Currently at Floor {elevator.CurrentFloor}, State: {elevator.State}, Dir: {elevator.CurrentDirection})");
            elevator.Step();
            _logger.LogTrace($"After Step, Elevator{elevator.Id} is now at Floor {elevator.CurrentFloor}, State: {elevator.State}, Dir: {elevator.CurrentDirection}");
            Console.WriteLine($"After Step, Elevator{elevator.Id} is now at Floor {elevator.CurrentFloor}, State: {elevator.State}, Dir: {elevator.CurrentDirection}");
        }

        if (!_floors?.Any() ?? true)
        {
            _logger.LogDebug("Phase 2: Skipping passenger loading as there are no floors defined.");
            _logger.LogInformation("--- Building Step Simulation Tick Complete (No loading phase) ---");
            Console.WriteLine("Phase 2: Skipping passenger loading as there are no floors defined.");
            Console.WriteLine("--- Building Step Simulation Tick Complete (No loading phase) ---");
            return;
        }

        _logger.LogDebug("Phase 2: Processing passenger loading for elevators with open doors.");
        foreach (IElevator elevatorForLoading in _elevators)
        {
            if (elevatorForLoading.State == ElevatorState.DoorsOpen)
            {
                _logger.LogDebug($"Elevator{elevatorForLoading.Id} has doors open at Floor {elevatorForLoading.CurrentFloor}. Checking for passengers to load.");
                Console.WriteLine($"Elevator{elevatorForLoading.Id} has doors open at Floor {elevatorForLoading.CurrentFloor}. Checking for passengers to load.");

                IFloor? currentElevatorFloor = _floors!.FirstOrDefault(f => f.FloorNumber == elevatorForLoading.CurrentFloor);

                if (currentElevatorFloor == null)
                {
                    _logger.LogWarning($"StepSimulation/Loading: Elevator{elevatorForLoading.Id} is at F{elevatorForLoading.CurrentFloor}, which does not exist in building's floor list. Cannot load passengers.");
                    continue;
                }

                Direction serviceDirection = elevatorForLoading.CurrentDirection;
                List<Person> boardedThisStopOnThisElevator = new List<Person>();

                if (serviceDirection == Direction.Up ||
                    (serviceDirection == Direction.Stopped && currentElevatorFloor.PeopleWaitingUp.Any()))
                {
                    while (currentElevatorFloor.PeopleWaitingUp.Any() &&
                           elevatorForLoading.Passengers.Count < elevatorForLoading.MaxCapacity)
                    {
                        Person? personToBoard = currentElevatorFloor.GetNextWaitingPerson(Direction.Up);
                        if (personToBoard == null) break;

                        if (elevatorForLoading.AddPassenger(personToBoard))
                        {
                            boardedThisStopOnThisElevator.Add(personToBoard);
                            _logger.LogInformation($"Elevator{elevatorForLoading.Id} boarded Person {personToBoard.Id} (Origin Floor:{personToBoard.OriginFloor} -> Destination Floor:{personToBoard.DestinationFloor}) at Floor {elevatorForLoading.CurrentFloor} going UP. Load: {elevatorForLoading.Passengers.Count}/{elevatorForLoading.MaxCapacity}");
                        }
                        else
                        {
                            _logger.LogWarning($"Elevator{elevatorForLoading.Id} AddPassenger failed for Person {personToBoard.Id} at Floor {elevatorForLoading.CurrentFloor}. Re-queuing person.");
                            currentElevatorFloor.AddWaitingPerson(personToBoard);
                            break;
                        }
                    }
                }

                bool canConsiderLoadingDown = serviceDirection == Direction.Down ||
                                             (serviceDirection == Direction.Stopped &&
                                              !boardedThisStopOnThisElevator.Any(p => p.DestinationFloor > p.OriginFloor) &&
                                              currentElevatorFloor.PeopleWaitingDown.Any());

                if (canConsiderLoadingDown)
                {
                    while (currentElevatorFloor.PeopleWaitingDown.Any() &&
                           elevatorForLoading.Passengers.Count < elevatorForLoading.MaxCapacity)
                    {
                        Person? personToBoard = currentElevatorFloor.GetNextWaitingPerson(Direction.Down);
                        if (personToBoard == null) break;

                        if (elevatorForLoading.AddPassenger(personToBoard))
                        {
                            _logger.LogInformation($"Elevator{elevatorForLoading.Id} boarded P{personToBoard.Id} (O:{personToBoard.OriginFloor}->D:{personToBoard.DestinationFloor}) at Floor {elevatorForLoading.CurrentFloor} going DOWN. Load: {elevatorForLoading.Passengers.Count}/{elevatorForLoading.MaxCapacity}");
                        }
                        else
                        {
                            _logger.LogWarning($"Elevator{elevatorForLoading.Id} AddPassenger failed for P{personToBoard.Id} at Floor {elevatorForLoading.CurrentFloor}. Re-queuing person.");
                            currentElevatorFloor.AddWaitingPerson(personToBoard);
                            break;
                        }
                    }
                }
            }
        }
        _logger.LogInformation("--- Building Step Simulation Tick Complete ---");
        Console.WriteLine("--- Building Step Simulation Tick Complete ---");
    }

    /// <summary>
    /// Displays the current status of all elevators and floors in the building.
    /// </summary>
    public void DisplayStatus()
    {
        string formatted = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss");
        Console.WriteLine($"Building Status at: {formatted}:");
        Console.WriteLine("----------------------------------------------------------------------");
        _logger.LogInformation($"Building Status at: {formatted}:");
        if (!_elevators?.Any() ?? true)
        {
            Console.WriteLine("Building not initialised.  No Elevators.");
            _logger.LogWarning("Building not initialised.  No Elevators.");
        }
        else
        {
            foreach (IElevator elevator in _elevators!)
            {
                Console.WriteLine(elevator.ToString());
                _logger.LogInformation(elevator.ToString());
            }

        }
        Console.WriteLine();
        if (!_floors?.Any() ?? true)
        {
            Console.WriteLine("Building not initialised. No Floors.");
            _logger.LogWarning("Building not initialised. No Floors.");
        }
        else
        {
            foreach (IFloor floor in _floors!)
            {
                Console.WriteLine(floor.ToString());
                _logger.LogInformation(floor.ToString());
            }

        }
        Console.WriteLine();
    }
}