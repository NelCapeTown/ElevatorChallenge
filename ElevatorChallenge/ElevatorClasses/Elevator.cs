using ElevatorChallenge.Enums;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Represents an elevator interface that defines the basic operations and properties of an elevator.
/// </summary>
public interface IElevator
{
    /// <summary>
    /// Gets the current direction of travel for the elevator.
    /// </summary>
    Direction CurrentDirection { get; }

    /// <summary>
    /// Gets the current floor where the elevator is located.
    /// </summary>
    int CurrentFloor { get; }

    /// <summary>
    /// Gets the unique identifier for this elevator instance.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the maximum number of people the elevator can carry at one time.
    /// </summary>
    int MaxCapacity { get; }

    /// <summary>
    /// Gets the list of people currently riding in the elevator.
    /// </summary>
    List<Person> Passengers { get; }

    /// <summary>
    /// Gets the current operational state of the elevator.
    /// </summary>
    ElevatorState State { get; }
    
    /// <summary>
    /// Keeps track of all upcoming stops for the elevator, including both passenger drop-off floors and assigned pickup floors.
    /// </summary>
    IReadOnlyCollection<int> AllUpcomingStops { get; }

    /// <summary>
    /// Adds a floor to the elevator's destination list (e.g., internal button press or assigned call).
    /// </summary>
    /// <param name="floor">The floor to add as a destination.</param>
    /// <param name="callDirection">The direction of the call, used for pickups. Defaults to Stopped.</param>
    void AddDestination(int floor, Direction callDirection = Direction.Stopped);

    /// <summary>
    /// Adds a passenger if capacity allows.
    /// </summary>
    /// <param name="person">The person to add as a passenger.</param>
    /// <returns>True if the passenger was added; otherwise, false.</returns>
    bool AddPassenger(Person person);

    /// <summary>
    /// Gets the number of pickup requests at the specified floor.
    /// </summary>
    /// <param name="floor">The floor for which to count pickup requests.</param>
    /// <returns>The number of pickup requests at the given floor.</returns>
    int GetPickupRequestCountAtFloor(int floor);

    /// <summary>
    /// Determines whether the elevator is currently idle (not moving and not handling any requests).
    /// </summary>
    /// <returns>True if the elevator is idle; otherwise, false.</returns>
    bool IsIdle();

    /// <summary>
    /// Determines whether the elevator is moving towards the specified floor in the required direction.
    /// </summary>
    /// <param name="floor">The floor to check if the elevator is moving towards.</param>
    /// <param name="requiredDirection">The direction in which the elevator should be moving.</param>
    /// <returns>True if the elevator is moving in the required direction and towards the specified floor; otherwise, false.</returns>
    bool IsMovingTowards(int floor, Direction requiredDirection);

    /// <summary>
    /// Simulates one step of elevator operation.
    /// </summary>
    void Step();

    /// <summary>
    /// Returns a string that represents the current state of the elevator.
    /// </summary>
    /// <returns>A string representation of the elevator's current status.</returns>
    string ToString();

    /// <summary>
    /// Removes passengers destined for the current floor.
    /// </summary>
    void UnloadPassengers();
}

/// <summary>
/// Represents a single elevator in the building.
/// </summary>
public class Elevator : IElevator
{
    private readonly ILogger<Elevator> _logger;
    private SortedSet<int> _passengerDropOffFloors = new SortedSet<int>();

    // Stores tuples of (floorNumber, directionOfCall) for pickups assigned by the Building
    private List<(int Floor, Direction CallDirection)> _assignedPickups = new List<(int, Direction)>();

    /// <summary>
    /// Gets the unique identifier for this elevator instance.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the current floor where the elevator is located.
    /// </summary>
    public int CurrentFloor { get; internal set; }

    /// <summary>
    /// Gets the current direction of travel for the elevator (Up, Down, or Stopped).
    /// </summary>
    public Direction CurrentDirection { get; internal set; }

    /// <summary>
    /// Gets the current operational state of the elevator (e.g., Moving, Stopped, DoorsOpen, OutOfService).
    /// </summary>
    public ElevatorState State { get; internal set; }

    /// <summary>
    /// Gets the list of people currently riding in the elevator.
    /// </summary>
    public List<Person> Passengers { get; }

    // IElevator property implementation
    /// <summary>
    /// Gets a collection of all upcoming stops for the elevator, including both passenger drop-off floors  and assigned
    /// pickup floors, sorted in ascending order.
    /// </summary>
    public IReadOnlyCollection<int> AllUpcomingStops
    {
        get
        {
            var allStops = new SortedSet<int>(_passengerDropOffFloors); // Start with passenger destinations
            foreach (var pickup in _assignedPickups)
            {
                allStops.Add(pickup.Floor); // Add pickup floors
            }
            return allStops; // SortedSet itself implements IReadOnlyCollection<int>
        }
    }

    /// <summary>
    /// Gets the maximum number of people the elevator can carry at one time.
    /// </summary>
    public int MaxCapacity { get; }

    // Stores floors this elevator is requested to visit (both for pickups and drop-offs)
    // Using a SortedSet helps in efficiently determining the next stop.
    private SortedSet<int> _destinationFloors;
    // For pickups, we also need to know the direction of the call
    private List<Tuple<int, Direction>> _pickupRequests;
    /// Static counter to assign unique IDs to elevators
    private static int _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Elevator"/> class with a unique identifier,
    /// a specified starting floor, and a maximum passenger capacity.
    /// The elevator is initialized in the stopped state with no passengers and no pending destinations.
    /// </summary>
    /// <param name="logger">The logger instance for this elevator.</param>
    /// <param name="startingFloor">The floor where the elevator will be initially positioned. Defaults to 1 if not specified.</param>
    /// <param name="maxCapacity">The maximum number of people the elevator can carry at one time. Defaults to 10 if not specified.</param>
    public Elevator(ILogger<Elevator> logger, int startingFloor = 1, int maxCapacity = 10)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        Id = _nextId++;
        CurrentFloor = startingFloor;
        CurrentDirection = Direction.Stopped;
        State = ElevatorState.Stopped;
        Passengers = new List<Person>();
        MaxCapacity = maxCapacity;
        _destinationFloors = new SortedSet<int>();
        _pickupRequests = new List<Tuple<int, Direction>>();
        _logger = logger;
    }

    /// <summary>
    /// Moves the elevator one floor in its current direction.
    /// Updates CurrentFloor and handles arrival at a destination.
    /// </summary>
    private void Move()
    {
        try
        {
            if (State != ElevatorState.Moving)
                return;

            // Determine primary target floor (could be a drop-off or a pickup)
            int? targetFloor = GetNextTargetFloor();

            if (!targetFloor.HasValue)
            {
                // No destinations, should become idle.
                // This case should ideally be caught before calling Move by Step()
                CurrentDirection = Direction.Stopped;
                State = ElevatorState.Stopped;
                Console.WriteLine($"Elevator {Id}: No target floor. Becoming Idle at {CurrentFloor}.");
                return;
            }

            // Set direction based on the first target floor
            if (targetFloor.Value > CurrentFloor)
                CurrentDirection = Direction.Up;
            else if (targetFloor.Value < CurrentFloor)
                CurrentDirection = Direction.Down;
            else // Already at the target floor
            {
                ArriveAtFloor(targetFloor.Value);
                return;
            }

            // Actually move
            if (CurrentDirection == Direction.Up)
                CurrentFloor++;
            else if (CurrentDirection == Direction.Down)
                CurrentFloor--;

            Console.WriteLine($"Elevator {Id}: Moving {CurrentDirection} to floor {CurrentFloor}. Target: {targetFloor.Value}");

            // Check if arrived at any destination floor (pickup or drop-off)
            if (ShouldStopAt(CurrentFloor))
            {
                ArriveAtFloor(CurrentFloor);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Elevator: {Id} Error during Move operation.");
            throw;
        }
    }
    private bool ShouldStopAt(int floor)
    {
        try
        {
            // Stop if it's a passenger's destination
            if (Passengers.Any(p => p.DestinationFloor == floor))
                return true;
            // Stop if it's a pickup request in the current direction of travel
            if (_pickupRequests.Any(req => req.Item1 == floor && (req.Item2 == CurrentDirection || CurrentDirection == Direction.Stopped)))
                return true;
            // Stop if it's a destination floor explicitly added (e.g. someone inside pressed a button)
            if (_destinationFloors.Contains(floor))
                return true;

            return false;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Elevator: {Id} Error during ShouldStopAt.");
            throw;
        }
    }

    private void ArriveAtFloor(int floor)
    {
        Console.WriteLine($"Elevator {Id}: Arrived at floor {floor}. Opening doors.");
        CurrentFloor = floor; // Ensure current floor is set correctly
        State = ElevatorState.DoorsOpen;

        // Remove this floor from general destinations and specific pickup requests
        _destinationFloors.Remove(floor);
        _pickupRequests.RemoveAll(req => req.Item1 == floor);

        // Actual loading/unloading logic will be called here or in Step()
        // For now, just log. Unload is handled by Step() before Move()
    }

    /// <summary>
    /// Gets the next logical target floor based on current direction and requests.
    /// This is a simplified version. A more advanced one would prioritize.
    /// </summary>
    private int? GetNextTargetFloor()
    {
        if (CurrentDirection == Direction.Up)
        {
            // Prefer current direction destinations or pickups
            int? nextUp = _destinationFloors.FirstOrDefault(f => f > CurrentFloor);
            int? nextPickupUp = _pickupRequests.Where(r => r.Item1 > CurrentFloor && r.Item2 == Direction.Up)
                                             .OrderBy(r => r.Item1).Select(r => (int?)r.Item1).FirstOrDefault();
            if (nextUp.HasValue && (!nextPickupUp.HasValue || nextUp.Value <= nextPickupUp.Value))
                return nextUp.Value;
            if (nextPickupUp.HasValue)
                return nextPickupUp.Value;
            // If no more up requests, check for any other requests (could be down or already passed)
            return _destinationFloors.Min(); // Or _pickupRequests.Select(r=>r.Item1).Min();
        }
        if (CurrentDirection == Direction.Down)
        {
            // Prefer current direction destinations or pickups
            int? nextDown = _destinationFloors.LastOrDefault(f => f < CurrentFloor);
            int? nextPickupDown = _pickupRequests.Where(r => r.Item1 < CurrentFloor && r.Item2 == Direction.Down)
                                             .OrderByDescending(r => r.Item1).Select(r => (int?)r.Item1).FirstOrDefault();

            if (nextDown.HasValue && nextDown.Value != 0 && (!nextPickupDown.HasValue || nextDown.Value >= nextPickupDown.Value))
                return nextDown.Value; // Check for 0 if floors are 1-indexed
            if (nextPickupDown.HasValue)
                return nextPickupDown.Value;

            return _destinationFloors.Max(); // Or _pickupRequests.Select(r=>r.Item1).Max();
        }
        // If Idle, pick the closest or first available request.
        if (_destinationFloors.Any())
            return _destinationFloors.First();
        if (_pickupRequests.Any())
            return _pickupRequests.First().Item1;

        return null;
    }

    /// <summary>
    /// Adds a passenger if capacity allows.
    /// </summary>
    /// <param name="person">The person to add as a passenger.</param>
    /// <returns>True if the passenger was added; otherwise, false.</returns>
    public bool AddPassenger(Person person)
    {
        if (Passengers.Count < MaxCapacity)
        {
            Passengers.Add(person);
            _destinationFloors.Add(person.DestinationFloor); // Add person's destination
            Console.WriteLine($"Elevator {Id}: {person} boarded at floor {CurrentFloor}. Passengers: {Passengers.Count}/{MaxCapacity}.");
            return true;
        }
        Console.WriteLine($"Elevator {Id}: Full. Cannot board {person} at floor {CurrentFloor}. Passengers: {Passengers.Count}/{MaxCapacity}.");
        return false;
    }

    /// <summary>
    /// Adds a floor to the elevator's destination list (e.g., internal button press or assigned call).
    /// </summary>
    /// <param name="floor">The floor to add as a destination.</param>
    /// <param name="callDirection">The direction of the call, used for pickups. Defaults to Stopped.</param>
    public void AddDestination(int floor, Direction callDirection = Direction.Stopped)
    {
        _destinationFloors.Add(floor);
        if (callDirection != Direction.Stopped) // This is a pickup request
        {
            if (!_pickupRequests.Any(r => r.Item1 == floor && r.Item2 == callDirection))
            {
                _pickupRequests.Add(Tuple.Create(floor, callDirection));
            }
        }

        // If idle, set initial direction towards this new destination
        if (State == ElevatorState.Stopped && CurrentDirection == Direction.Stopped)
        {
            if (floor > CurrentFloor)
                CurrentDirection = Direction.Up;
            else if (floor < CurrentFloor)
                CurrentDirection = Direction.Down;
            // If floor == CurrentFloor, it should open doors (handled by Step)
            State = ElevatorState.Moving; // Will immediately check if it needs to stop or move
        }
        Console.WriteLine($"Elevator {Id}: Destination {floor} (Dir: {callDirection}) added. Current destinations: {string.Join(", ", _destinationFloors)} Pickups: {string.Join(", ", _pickupRequests.Select(r => $"F{r.Item1}-{r.Item2}"))}");
    }

    /// <summary>
    /// Determines whether the elevator is currently idle (not moving and not handling any requests).
    /// </summary>
    /// <returns>True if the elevator is idle; otherwise, false.</returns>
    public bool IsIdle() => State == ElevatorState.Stopped;

    /// <summary>
    /// Determines whether the elevator is moving towards the specified floor in the required direction.
    /// </summary>
    /// <param name="floor">The floor to check if the elevator is moving towards.</param>
    /// <param name="requiredDirection">The direction in which the elevator should be moving.</param>
    /// <returns>True if the elevator is moving in the required direction and towards the specified floor; otherwise, false.</returns>
    public bool IsMovingTowards(int floor, Direction requiredDirection)
    {
        if (State == ElevatorState.Moving)
        {
            if (CurrentDirection == Direction.Up && requiredDirection == Direction.Up && floor >= CurrentFloor)
                return true;
            if (CurrentDirection == Direction.Down && requiredDirection == Direction.Down && floor <= CurrentFloor)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the number of pickup requests at the specified floor.
    /// </summary>
    /// <param name="floor">The floor for which to count pickup requests.</param>
    /// <returns>The number of pickup requests at the given floor.</returns>
    public int GetPickupRequestCountAtFloor(int floor)
    {
        return _pickupRequests.Count(pr => pr.Item1 == floor);
    }

    /// <summary>
    /// Returns a string that represents the current state of the elevator, including its floor, direction, state, passenger count, stops, and pickup requests.
    /// </summary>
    /// <returns>A string representation of the elevator's current status.</returns>
    public override string ToString()
    {
        return $"Elevator {Id}: Floor {CurrentFloor}, Dir: {CurrentDirection}, State: {State}, People: {Passengers.Count}/{MaxCapacity}, Stops: [{string.Join(",", _destinationFloors)}], Pickups: [{string.Join(", ", _pickupRequests.Select(r => $"F{r.Item1}({r.Item2.ToString().First()})"))}]";
    }
    // This method needs to be implemented carefully. It's the core of the elevator's movement logic.
    private void RecalculateNextStopAndDirection()
    {
        // If doors open, don't change mind yet
        if (State == ElevatorState.DoorsOpen) return;

        int? nextStop = DetermineNextLogicalStop(); // This method will use AllUpcomingStops or the individual lists

        if (nextStop.HasValue)
        {
            if (CurrentFloor == nextStop.Value)
            {
                // Already at a target stop. Step() should handle opening doors if needed.
                // Direction might become Idle or stay if continuing after pickup.
                // For simplicity, if at target, mark as Idle for now, Step() refines.
                // CurrentDirection = DetermineDirectionAfterStop(nextStop.Value); // More complex
            }
            else if (nextStop.Value > CurrentFloor)
            {
                //CurrentDirection = Direction.Up;
                //State = ElevatorState.Moving;
            }
            else // nextStop.Value < CurrentFloor
            {
                //CurrentDirection = Direction.Down;
                //State = ElevatorState.Moving;
            }
            _logger.LogTrace("E{Id}: Recalculated. Next target stop is F{NextStop}. CurrentDir: {CurrentDir}, State: {State}",
               Id,nextStop,CurrentDirection,State);
        }
        else
        {
            // No more stops (neither drop-offs nor pickups)
            //CurrentDirection = Direction.Idle;
            //State = ElevatorState.Idle;
            _logger.LogTrace("E{Id}: Recalculated. No upcoming stops. Becoming Idle.",Id);
        }
        // The actual setting of CurrentDirection and State should ideally happen in Step()
        // based on the outcome of DetermineNextLogicalStop() and current state.
        // RecalculateNextStopAndDirection primarily finds the *next target*.
    }

    // This is the critical logic.
    private int? DetermineNextLogicalStop()
    {
        var allStops = AllUpcomingStops; // Use the combined list for decision making
        if (!allStops.Any()) return null;

        // --- "Look-ahead" or "SCAN" like algorithm ---
        // 1. Continue in CurrentDirection if there are stops (pickups or drop-offs) in that direction.
        // 2. If no stops in current direction, but stops exist in opposite direction, prepare to change.
        // 3. If Idle, pick best target.

        IEnumerable<int> potentialStops;

        if (CurrentDirection == Direction.Up)
        {
            potentialStops = allStops.Where(f => f >= CurrentFloor).OrderBy(f => f);
            if (potentialStops.Any())
            {
                // If current floor is a stop, service it first.
                if (potentialStops.First() == CurrentFloor && (_passengerDropOffFloors.Contains(CurrentFloor) || _assignedPickups.Any(p => p.Floor == CurrentFloor && p.CallDirection == Direction.Up)))
                {
                    return CurrentFloor;
                }
                // Else, next stop upwards
                return potentialStops.Where(f => f > CurrentFloor).FirstOrDefault();
            }
            // No more stops upwards or at current floor, look for any stops downwards (take the highest one to reverse towards)
            return allStops.OrderByDescending(f => f).FirstOrDefault(); // Pick highest overall if reversing
        }
        else if (CurrentDirection == Direction.Down)
        {
            potentialStops = allStops.Where(f => f <= CurrentFloor).OrderByDescending(f => f);
            if (potentialStops.Any())
            {
                if (potentialStops.First() == CurrentFloor && (_passengerDropOffFloors.Contains(CurrentFloor) || _assignedPickups.Any(p => p.Floor == CurrentFloor && p.CallDirection == Direction.Down)))
                {
                    return CurrentFloor;
                }
                return potentialStops.Where(f => f < CurrentFloor).FirstOrDefault();
            }
            // No more stops downwards, look for any stops upwards (take the lowest one to reverse towards)
            return allStops.OrderBy(f => f).FirstOrDefault(); // Pick lowest overall if reversing
        }
        else // CurrentDirection is Idle (or Stopped)
        {
            // Find the closest stop among all upcoming stops
            if (!allStops.Any()) return null;
            return allStops.OrderBy(f => Math.Abs(f - CurrentFloor)).ThenBy(f => f).First(); // ThenBy to prefer lower floors in case of tie in distance
        }
    }

    // ToDo: Tidy this up and ensure it fits the overall logic.
    // Other IElevator methods (Step, UnloadPassengers, etc.) would use these internal lists.
    // Example:
    /// <summary>
    /// Unloads passengers from the elevator who have reached their destination floor.
    /// </summary>
    /// <remarks>This method removes passengers whose <see cref="Person.DestinationFloor"/> matches the elevator's
    /// current floor. If all passengers for the current floor are unloaded, the floor is removed from the list of
    /// drop-off destinations. The method also recalculates the next stop and direction for the elevator after unloading
    /// passengers.</remarks>
    public void UnloadPassengers()
    {
        // Only unload if doors are open or should be opening (logic in Step will manage State)
        // if (State != ElevatorState.DoorsOpen) return;

        var arrivedPassengers = Passengers.Where(p => p.DestinationFloor == CurrentFloor).ToList();
        if (arrivedPassengers.Any())
        {
            _logger.LogInformation($"Elevator {Id}: Unloading {arrivedPassengers.Count} passengers at Floor {CurrentFloor}.");
            foreach (var person in arrivedPassengers)
            {
                Passengers.Remove(person);
                // No need to remove from _passengerDropOffFloors here if we re-evaluate stops.
                // However, it's cleaner to remove it once serviced for that specific instance of travel.
                // But if another passenger for the same floor boards later, it needs to be re-added.
                // Let's assume _passengerDropOffFloors tracks *currently desired* drop-offs.
            }
            // Once all passengers for this floor are off, that floor MIGHT no longer be a drop-off destination
            // unless another passenger currently in the elevator is also going there (unlikely if we remove all).
            if (!Passengers.Any(p => p.DestinationFloor == CurrentFloor))
            {
                _passengerDropOffFloors.Remove(CurrentFloor);
                _logger.LogDebug("E{Id}: F{CurrentFloor} removed from passenger drop-off destinations. DropOffs: [{Dests}]",Id,CurrentFloor,string.Join(",",_passengerDropOffFloors));
            }
            RecalculateNextStopAndDirection();
        }
    }

    /// <summary>
    /// Primary method to simulate one step of elevator operation.
    /// This will involve moving, opening/closing doors, loading/unloading passengers.
    /// </summary>
    public void Step()
    {
        try
        {
            // 1. Handle Doors (Open/Close based on State)
            if (State == ElevatorState.DoorsOpen)
            {
                // Simulate door open duration or immediately close if done
                Console.WriteLine($"Elevator {Id}: Doors closing at floor {CurrentFloor}.");
                _logger.LogInformation($"Elevator {Id}: Doors closing at floor {CurrentFloor}.");
                State = ElevatorState.Stopped; // Or Moving if it has next destination
                                               // Actual passenger loading/unloading would happen before closing
            }

            // 2. Unload Passengers if at their destination floor
            UnloadPassengers();

            // 3. Load Passengers if stopped at a floor with requests matching direction
            // This would typically be triggered by the Building/Controller
            // For now, we assume it's handled externally or after deciding to stop.

            // 4. Determine Next Action (Move or become Idle)
            if (_destinationFloors.Any() || _pickupRequests.Any())
            {
                if (State != ElevatorState.Moving && State != ElevatorState.DoorsOpen)
                {
                    State = ElevatorState.Moving;
                }

                Move();
            }
            else
            {
                if (State == ElevatorState.Moving) // If it was moving but no more destinations
                {
                    Console.WriteLine($"Elevator {Id}: Reached final destination or cleared requests. Becoming Idle at floor {CurrentFloor}.");
                    _logger.LogInformation($"Elevator {Id}: Reached final destination or cleared requests. Becoming Idle at floor {CurrentFloor}.");
                }
                State = ElevatorState.Stopped;
                CurrentDirection = Direction.Stopped;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,$"Elevator {Id}: Error during Step operation.");
            Console.WriteLine($"Elevator {Id}: Error during Step operation: {ex.Message}");
            State = ElevatorState.OutOfService; // Set to a non-operational state
        }
    }

}

