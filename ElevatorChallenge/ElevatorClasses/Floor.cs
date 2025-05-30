using ElevatorChallenge.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Represents the contract for a floor in the building.
/// </summary>
public interface IFloor
{
    /// <summary>
    /// Gets the number of this floor within the building.
    /// </summary>
    int FloorNumber { get; }

    /// <summary>
    /// Gets the list of people currently waiting on this floor to go down.
    /// </summary>
    List<Person> PeopleWaitingDown { get; }

    /// <summary>
    /// Gets the list of people currently waiting on this floor to go up.
    /// </summary>
    List<Person> PeopleWaitingUp { get; }

    /// <summary>
    /// Adds a person to the appropriate waiting list (up or down) based on their destination floor.
    /// </summary>
    /// <param name="person">The person to add to the waiting list.</param>
    void AddWaitingPerson(Person person);

    /// <summary>
    /// Retrieves and removes the next person waiting in the specified direction.
    /// </summary>
    /// <param name="direction">The direction (up or down) in which the person wants to travel.</param>
    /// <returns>The next <see cref="Person"/> waiting in the specified direction, or <c>null</c> if none are waiting.</returns>
    Person? GetNextWaitingPerson(Direction direction);

    /// <summary>
    /// Returns a string that represents the current state of the floor.
    /// </summary>
    /// <returns>A string representation of the floor's current status.</returns>
    string ToString();

    /// <summary>
    /// Gets the number of people currently waiting in the specified direction.
    /// </summary>
    /// <param name="direction">The direction (up or down) to count waiting people for.</param>
    /// <returns>The number of people waiting in the specified direction.</returns>
    int WaitingCount(Direction direction);
}

/// <summary>
/// Represents a floor in the building.
/// </summary>
public class Floor : IFloor
{
    private readonly ILogger<Floor> _logger;

    /// <inheritdoc/>
    public int FloorNumber { get; }

    /// <inheritdoc/>
    public List<Person> PeopleWaitingUp { get; }

    /// <inheritdoc/>
    public List<Person> PeopleWaitingDown { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Floor"/> class with the specified floor number.
    /// </summary>
    /// <param name="logger">The logger instance for this floor.</param>
    /// <param name="floorNumber">The number representing this floor in the building.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is null.</exception>
    public Floor(ILogger<Floor> logger, int floorNumber)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        FloorNumber = floorNumber;
        PeopleWaitingUp = new List<Person>();
        PeopleWaitingDown = new List<Person>();
    }

    /// <inheritdoc/>
    public void AddWaitingPerson(Person person)
    {
        try
        {
            if (person.DestinationFloor > FloorNumber)
            {
                PeopleWaitingUp.Add(person);
                _logger.LogInformation($"{person} is now waiting on floor {FloorNumber} to go UP.");
                Console.WriteLine($"{person} is now waiting on floor {FloorNumber} to go UP.");
            }
            else if (person.DestinationFloor < FloorNumber)
            {
                PeopleWaitingDown.Add(person);
                _logger.LogInformation($"{person} is now waiting on floor {FloorNumber} to go DOWN.");
                Console.WriteLine($"{person} is now waiting on floor {FloorNumber} to go DOWN.");
            }
            // If DestinationFloor == FloorNumber, they are already there.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Floor: {FloorNumber} Error in AddWaitingPerson.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Person? GetNextWaitingPerson(Direction direction)
    {
        try
        {
            Person? person = null;
            if (direction == Direction.Up && PeopleWaitingUp.Any())
            {
                person = PeopleWaitingUp.First();
                PeopleWaitingUp.RemoveAt(0);
            }
            else if (direction == Direction.Down && PeopleWaitingDown.Any())
            {
                person = PeopleWaitingDown.First();
                PeopleWaitingDown.RemoveAt(0);
            }
            return person;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Floor: {FloorNumber} Error in GetNextWaitingPerson.");
            throw;
        }
    }

    /// <inheritdoc/>
    public int WaitingCount(Direction direction) => direction == Direction.Up ? PeopleWaitingUp.Count : PeopleWaitingDown.Count;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Floor {FloorNumber}: Waiting Up: {PeopleWaitingUp.Count}, Waiting Down: {PeopleWaitingDown.Count}";
    }
}

