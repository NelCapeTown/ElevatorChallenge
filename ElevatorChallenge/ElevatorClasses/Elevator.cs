using ElevatorChallenge.Enums;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Represents the contract for an elevator in the building.
/// </summary>
public interface IElevator
{
    /// <summary>
    /// Gets the current direction of the elevator.
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
    /// Gets the maximum number of passengers the elevator can carry.
    /// </summary>
    int MaxCapacity { get; }

    /// <summary>
    /// Gets the list of passengers currently in the elevator.
    /// </summary>
    List<Person> Passengers { get; }

    /// <summary>
    /// Gets the current operational state of the elevator.
    /// </summary>
    ElevatorState State { get; }

    /// <summary>
    /// Gets a read-only collection of all upcoming stops for the elevator.
    /// </summary>
    IReadOnlyCollection<int> AllUpcomingStops { get; }

    /// <summary>
    /// Adds a destination floor to the elevator's list of stops.
    /// </summary>
    /// <param name="floor">The floor to add as a destination.</param>
    /// <param name="callDirection">The direction of the call, if applicable.</param>
    void AddDestination(int floor,Direction callDirection = Direction.Stopped);

    /// <summary>
    /// Attempts to add a passenger to the elevator.
    /// </summary>
    /// <param name="person">The person to add.</param>
    /// <returns>True if the passenger was added; otherwise, false.</returns>
    bool AddPassenger(Person person);

    /// <summary>
    /// Gets the number of pickup requests at the specified floor.
    /// </summary>
    /// <param name="floor">The floor to check for pickup requests.</param>
    /// <returns>The number of pickup requests at the floor.</returns>
    int GetPickupRequestCountAtFloor(int floor);

    /// <summary>
    /// Determines whether the elevator is currently stopped.
    /// </summary>
    /// <returns>True if the elevator is stopped; otherwise, false.</returns>
    bool IsStopped();

    /// <summary>
    /// Determines whether the elevator is moving towards the specified floor in the required direction.
    /// </summary>
    /// <param name="floor">The floor to check.</param>
    /// <param name="requiredDirection">The direction required.</param>
    /// <returns>True if moving towards the floor in the required direction; otherwise, false.</returns>
    bool IsMovingTowards(int floor,Direction requiredDirection);

    /// <summary>
    /// Advances the elevator by one step in its operation.
    /// </summary>
    void Step();

    /// <summary>
    /// Returns a string that represents the current state of the elevator.
    /// </summary>
    /// <returns>A string representation of the elevator's current status.</returns>
    string ToString();

    /// <summary>
    /// Unloads passengers whose destination is the current floor.
    /// </summary>
    void UnloadPassengers();
}

/// <summary>
/// Represents an elevator in the building, managing its state, passengers, and movement.
/// </summary>
public class Elevator : IElevator
{
    private readonly ILogger<Elevator> _logger;
    private static int _nextId = 1;

    // --- Consolidated and Primary State Fields ---
    private readonly SortedSet<int> _passengerDropOffFloors = new SortedSet<int>();
    private readonly List<(int Floor, Direction CallDirection)> _assignedPickups = new List<(int Floor, Direction CallDirection)>();
    private Direction _previousTravelDirection = Direction.Stopped;

    /// <summary>
    /// Gets the unique identifier for this elevator instance.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets or sets the current floor where the elevator is located.
    /// </summary>
    public int CurrentFloor { get; internal set; }

    /// <summary>
    /// Gets or sets the current direction of the elevator.
    /// </summary>
    public Direction CurrentDirection { get; internal set; }

    /// <summary>
    /// Gets or sets the current operational state of the elevator.
    /// </summary>
    public ElevatorState State { get; internal set; }

    /// <summary>
    /// Gets the list of passengers currently in the elevator.
    /// </summary>
    public List<Person> Passengers { get; }

    /// <summary>
    /// Gets the maximum number of passengers the elevator can carry.
    /// </summary>
    public int MaxCapacity { get; }

    /// <summary>
    /// Gets a read-only collection of all upcoming stops for the elevator.
    /// </summary>
    public IReadOnlyCollection<int> AllUpcomingStops
    {
        get
        {
            var allStops = new SortedSet<int>(_passengerDropOffFloors);
            foreach (var pickup in _assignedPickups)
            {
                allStops.Add(pickup.Floor);
            }
            return allStops;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Elevator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for this elevator.</param>
    /// <param name="startingFloor">The floor where the elevator starts. Defaults to 1.</param>
    /// <param name="maxCapacity">The maximum number of passengers. Defaults to 10.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is null.</exception>
    public Elevator(ILogger<Elevator> logger,int startingFloor = 1,int maxCapacity = 10)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger),"Logger cannot be null.");
        Id = _nextId++;
        CurrentFloor = startingFloor;
        CurrentDirection = Direction.Stopped;
        State = ElevatorState.Stopped;
        Passengers = new List<Person>();
        MaxCapacity = maxCapacity;
        _logger.LogInformation("Elevator E{ElevatorId} created at F{StartingFloor}, Capacity: {MaxCapacity}.",Id,startingFloor,maxCapacity);
    }

    /// <summary>
    /// Adds a destination floor or pickup request to the elevator's list of stops.
    /// </summary>
    /// <param name="floor">The floor to add as a destination or pickup.</param>
    /// <param name="callDirection">The direction of the call, if applicable.</param>
    public void AddDestination(int floor,Direction callDirection = Direction.Stopped)
    {
        bool changed = false;
        if (callDirection == Direction.Up || callDirection == Direction.Down)
        {
            if (!_assignedPickups.Any(r => r.Floor == floor && r.CallDirection == callDirection))
            {
                _assignedPickups.Add((floor, callDirection));
                changed = true;
                _logger.LogInformation("E{Id}: Pickup request added for F{Floor} ({CallDirection}). AssignedPickups: [{Pickups}]",
                    Id,floor,callDirection,string.Join(";",_assignedPickups.Select(r => $"F{r.Floor}-{r.CallDirection}")));
            }
        }
        else
        {
            if (_passengerDropOffFloors.Add(floor))
            {
                changed = true;
                _logger.LogInformation("E{Id}: General destination F{Floor} added to passenger drop-off list. DropOffs: [{Dests}]",
                    Id,floor,string.Join(",",_passengerDropOffFloors));
            }
        }

        if (changed && (State == ElevatorState.Stopped || CurrentDirection == Direction.Stopped))
        {
            _logger.LogDebug("E{Id}: Destination added while potentially idle. Step() will re-evaluate.",Id);
        }
    }

    /// <summary>
    /// Attempts to add a passenger to the elevator.
    /// </summary>
    /// <param name="person">The person to add.</param>
    /// <returns>True if the passenger was added; otherwise, false.</returns>
    public bool AddPassenger(Person person)
    {
        if (Passengers.Count < MaxCapacity)
        {
            Passengers.Add(person);
            _passengerDropOffFloors.Add(person.DestinationFloor);
            _logger.LogInformation("E{Id}: P{PersonId} (O:{OriginFloor}->D:{DestFloor}) boarded at F{CurrentFloor}. Pax: {PaxCount}/{MaxCap}. DropOffs: [{DropOffs}]",
                Id,person.Id,person.OriginFloor,person.DestinationFloor,CurrentFloor,Passengers.Count,MaxCapacity,string.Join(",",_passengerDropOffFloors));
            return true;
        }
        _logger.LogWarning("E{Id}: Cannot board P{PersonId} at F{CurrentFloor}. Capacity full ({PaxCount}/{MaxCap}).",
            Id,person.Id,CurrentFloor,Passengers.Count,MaxCapacity);
        return false;
    }

    private void ArriveAtFloor(int floor)
    {
        _previousTravelDirection = CurrentDirection; // Capture travel direction before neutralizing
        _logger.LogInformation("E{Id}: Arrived at F{ArrivalFloor}. Processing arrival. Travelled {PrevTravelDir}.",Id,floor,_previousTravelDirection);

        CurrentFloor = floor; // Ensure CurrentFloor is accurately set
        State = ElevatorState.DoorsOpen;
        CurrentDirection = Direction.Stopped; // KEY CHANGE: Neutral direction for loading/idling
        _logger.LogDebug("E{Id}: At F{ArrivalFloor}, State set to DoorsOpen, Direction set to Stopped.",Id,floor);

        // Clear the specific *pickup* request that was serviced to reach this floor, if any.
        // Match based on floor and the direction the elevator was traveling to fulfill the pickup.
        var servicedPickup = _assignedPickups.FirstOrDefault(p => p.Floor == floor && p.CallDirection == _previousTravelDirection);
        if (servicedPickup != default)
        {
            _assignedPickups.Remove(servicedPickup);
            _logger.LogInformation("E{Id}: Serviced and removed pickup {PickupDirection} request for F{Floor}. Pickups left: {PickupCount}",
                Id,servicedPickup.CallDirection,floor,_assignedPickups.Count);
        }
        else
        {
            // If no specific directional pickup matched, check if ANY pickup was for this floor.
            // This might happen if elevator was idle and picked this floor as closest target.
            int removedCount = _assignedPickups.RemoveAll(p => p.Floor == floor);
            if (removedCount > 0)
            {
                _logger.LogInformation("E{Id}: Serviced and removed {RemovedCount} general pickup request(s) for F{Floor} as arrival context didn't match specific one.",Id,removedCount,floor);
            }
        }
        // UnloadPassengers will be called by Step() next, and it will handle _passengerDropOffFloors.
    }

    public void UnloadPassengers()
    {
        if (State != ElevatorState.DoorsOpen)
        {
            return;
        }

        var arrivedPassengers = Passengers.Where(p => p.DestinationFloor == CurrentFloor).ToList();
        if (arrivedPassengers.Any())
        {
            _logger.LogInformation("E{Id}: Unloading {PaxCount} passengers at F{CurrentFloor}.",Id,arrivedPassengers.Count,CurrentFloor);
            foreach (var person in arrivedPassengers)
            {
                Passengers.Remove(person);
            }

            if (!Passengers.Any(p => p.DestinationFloor == CurrentFloor))
            {
                if (_passengerDropOffFloors.Remove(CurrentFloor))
                {
                    _logger.LogInformation("E{Id}: F{CurrentFloor} removed from passenger drop-off list. DropOffs: [{Dests}]",Id,CurrentFloor,string.Join(",",_passengerDropOffFloors));
                }
            }
        }
    }

    /// <summary>
    /// Advances the elevator by one step in its operation, handling movement, stops, and passenger unloading.
    /// </summary>
    public void Step()
    {
        try
        {
            _logger.LogTrace("E{Id}: Step BEGIN. F{CurrentFloor}, St:{State}, Dir:{CurrentDir}, Pax:{PaxCount}, AllStops:[{AllStops}]",
                Id,CurrentFloor,State,CurrentDirection,Passengers.Count,string.Join(",",AllUpcomingStops));

            if (State == ElevatorState.DoorsOpen)
            {
                UnloadPassengers();
                _logger.LogInformation("E{Id}: Doors were open at F{CurrentFloor}. Closing doors.",Id,CurrentFloor);
                State = ElevatorState.Stopped;
            }

            int? nextTarget = DetermineNextLogicalStop();

            if (nextTarget.HasValue)
            {
                if (CurrentFloor == nextTarget.Value)
                {
                    if (State != ElevatorState.DoorsOpen)
                    {
                        _logger.LogInformation("E{Id}: At target F{CurrentFloor} with doors closed. Opening doors.",Id,CurrentFloor);
                        ArriveAtFloor(CurrentFloor);
                    }
                }
                else
                {
                    _previousTravelDirection = (nextTarget.Value > CurrentFloor) ? Direction.Up : Direction.Down;
                    State = ElevatorState.Moving;
                    MoveTowards(nextTarget.Value);
                }
            }
            else
            {
                if (State == ElevatorState.Moving)
                {
                    _logger.LogInformation("E{Id}: Was moving, but no more stops. Becoming Stopped at F{CurrentFloor}.",Id,CurrentFloor);
                }
                State = ElevatorState.Stopped;
                CurrentDirection = Direction.Stopped;
                _previousTravelDirection = Direction.Stopped;
            }
            _logger.LogTrace("E{Id}: Step END. F{CurrentFloor}, St:{State}, Dir:{CurrentDir}",Id,CurrentFloor,State,CurrentDirection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"E{Id}: Error during Step operation.",Id);
            State = ElevatorState.OutOfService;
            CurrentDirection = Direction.Stopped;
        }
    }

    private void MoveTowards(int targetFloor)
    {
        if (State != ElevatorState.Moving)
        {
            _logger.LogTrace("E{Id}: MoveTowards called but State is not Moving ({CurrentState}). Setting to Moving.",Id,State);
            State = ElevatorState.Moving; // Ensure it's set if Step logic determined a move.
        }

        if (CurrentFloor < targetFloor)
        {
            CurrentDirection = Direction.Up; // Travel direction
            CurrentFloor++;
            _logger.LogInformation("E{Id}: Moving {CurrentDirection} to F{CurrentFloor}. Overall Target: F{TargetFloor}",Id,CurrentDirection,CurrentFloor,targetFloor);
        }
        else if (CurrentFloor > targetFloor)
        {
            CurrentDirection = Direction.Down; // Travel direction
            CurrentFloor--;
            _logger.LogInformation("E{Id}: Moving {CurrentDirection} to F{CurrentFloor}. Overall Target: F{TargetFloor}",Id,CurrentDirection,CurrentFloor,targetFloor);
        }

        // After moving one floor, check if this new CurrentFloor is a required stop
        if (ShouldStopAt(CurrentFloor)) // Pass CurrentFloor after move
        {
            ArriveAtFloor(CurrentFloor); // This will open doors and set CurrentDirection to Stopped
        }
    }

    private bool ShouldStopAt(int floor) // Now uses consolidated lists
    {
        if (_passengerDropOffFloors.Contains(floor)) // Is it a drop-off for a current passenger?
            return true;

        // Is it an assigned pickup, and are we in a state/direction to service it?
        // CurrentDirection here is the *travel direction*.
        if (_assignedPickups.Any(req => req.Floor == floor &&
                                      (req.CallDirection == CurrentDirection || CurrentDirection == Direction.Stopped))) // Simpler check: stop if it's a pickup for this floor matching travel dir, or if we are stopped and it's a call
            return true;

        // If moving, and a pickup is at this floor for the *opposite* direction,
        // we generally wouldn't stop unless it's the only call or other complex logic.
        // For now, this ShouldStopAt is simpler. DetermineNextLogicalStop is smarter.
        // This method primarily answers: if I land on this floor, is it one of my active targets?
        return AllUpcomingStops.Contains(floor); // General catch-all: if it's in any list of stops.
    }

    private int? DetermineNextLogicalStop() // - This logic is good.
    {
        var allStops = AllUpcomingStops;
        if (!allStops.Any()) return null;

        Direction evalDirection = CurrentDirection;
        // If currently stopped, but was previously moving, try to continue that direction first if appropriate.
        if (evalDirection == Direction.Stopped && _previousTravelDirection != Direction.Stopped)
        {
            evalDirection = _previousTravelDirection;
        }

        int currentEvalFloor = CurrentFloor;

        if (evalDirection == Direction.Up)
        {
            var upcomingUpStops = allStops.Where(f => f >= currentEvalFloor).OrderBy(f => f).ToList();
            if (upcomingUpStops.Any())
            {
                // If current floor is a stop and elevator is heading up or was just stopped here from an up journey.
                if (upcomingUpStops.First() == currentEvalFloor) return currentEvalFloor;
                // Next stop strictly greater than current floor.
                return upcomingUpStops.FirstOrDefault(f => f > currentEvalFloor);
            }
            // No more UP stops in the current path, look for any stops below to reverse towards (highest one).
            return allStops.Any() ? allStops.Where(f => f < currentEvalFloor).OrderByDescending(f => f).FirstOrDefault() : null;
        }
        else if (evalDirection == Direction.Down)
        {
            var upcomingDownStops = allStops.Where(f => f <= currentEvalFloor).OrderByDescending(f => f).ToList();
            if (upcomingDownStops.Any())
            {
                if (upcomingDownStops.First() == currentEvalFloor) return currentEvalFloor;
                return upcomingDownStops.FirstOrDefault(f => f < currentEvalFloor);
            }
            // No more DOWN stops, look for any stops above to reverse towards (lowest one).
            return allStops.Any() ? allStops.Where(f => f > currentEvalFloor).OrderBy(f => f).FirstOrDefault() : null;
        }
        else // CurrentDirection is Stopped (and _previousTravelDirection was also Stopped, or no prev direction)
        {
            if (!allStops.Any()) return null;
            // Find the closest stop.
            return allStops.OrderBy(f => Math.Abs(f - currentEvalFloor))
                           .ThenBy(f => f)
                           .FirstOrDefault();
        }
    }

    // RecalculateNextStopAndDirection is mostly superseded by Step's direct use of DetermineNextLogicalStop
    // It was for nudging an idle elevator. AddPassenger/AddDestination might call this if elevator is Stopped.
    private void RecalculateNextStopAndDirection(bool attemptToSetMoving) //
    {
        if (State == ElevatorState.Stopped && CurrentDirection == Direction.Stopped)
        {
            int? nextStop = DetermineNextLogicalStop();
            if (nextStop.HasValue && attemptToSetMoving)
            {
                if (nextStop.Value == CurrentFloor)
                {
                    _logger.LogDebug("E{Id}: Recalculated. Target is current F{CurrentFloor}. Step() will handle opening doors.",Id,CurrentFloor);
                    // Step() will call ArriveAtFloor if doors aren't open
                }
                else
                {
                    // Let Step() handle the transition to Moving state and setting CurrentDirection
                    _logger.LogDebug("E{Id}: Recalculated. New target is F{NextStop}. Step() will initiate movement.",Id,nextStop.Value);
                }
            }
            else if (!nextStop.HasValue)
            {
                _logger.LogDebug("E{Id}: Recalculated. No upcoming stops. Remains Stopped.",Id);
            }
        }
    }

    public bool IsStopped() => State == ElevatorState.Stopped && CurrentDirection == Direction.Stopped; // [cite: 133]

    public bool IsMovingTowards(int floor,Direction requiredDirection) //
    {
        if (State == ElevatorState.Moving && CurrentDirection == requiredDirection)
        {
            return (requiredDirection == Direction.Up && floor >= CurrentFloor) ||
                   (requiredDirection == Direction.Down && floor <= CurrentFloor);
        }
        return false;
    }

    /// <summary>
    /// Gets the number of pickup requests at the specified floor.
    /// </summary>
    /// <param name="floor">The floor to check for pickup requests.</param>
    /// <returns>The number of pickup requests at the floor.</returns>
    public int GetPickupRequestCountAtFloor(int floor)
    {
        return _assignedPickups.Count(pr => pr.Floor == floor);
    }

    /// <summary>
    /// Returns a string that represents the current state of the elevator.
    /// </summary>
    /// <returns>A string representation of the elevator's current status.</returns>
    public override string ToString()
    {
        return $"Elevator E{Id}: F{CurrentFloor}, Dir:{CurrentDirection}, St:{State}, Pax:{Passengers.Count}/{MaxCapacity}, Stops:[{string.Join(",",AllUpcomingStops)}]";
    }
}