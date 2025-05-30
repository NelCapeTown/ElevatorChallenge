using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElevatorChallenge.Enums;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Represents a person waiting for or riding an elevator.
/// We will always create a new instance of Person when a person arrives at the elevator on an OriginFloor.
/// When the person enters the elevator and presses the button we will set the DestinationFloor.
/// </summary>
public class Person
{
    /// <summary>
    /// Gets the unique identifier for this person instance.
    /// </summary>
    public int Id
    {
        get;
    }

    /// <summary>
    /// Gets the floor where the person is initially waiting for the elevator.
    /// </summary>
    public int OriginFloor
    {
        get;
    }

    /// <summary>
    /// Gets the floor to which the person intends to travel.
    /// </summary>
    public int DestinationFloor
    {
        get; set;
    }

    private static int _nextId = 1;

    /// <summary>
    /// Creates a new instance of <see cref="Person"/> with an Id, OriginFloor, and DestinationFloor set.
    /// For the sake of simplifying the UI, we will set the OriginFloor and DestinationFloor when we 
    /// create the Person instance instead of waiting for the person to press the button in the elevator.
    /// </summary>
    /// <param name="originFloor">The floor on which the person is currently waiting for the elevator.</param>
    /// <param name="destinationFloor">The floor to which the person wants to go.</param>
    public Person(int originFloor, int destinationFloor)
    {
        Id = _nextId++;
        OriginFloor = originFloor;
        DestinationFloor = destinationFloor;
    }

    /// <summary>
    /// Returns a string that represents the current state of the person, including their Id, origin, and destination floors.
    /// </summary>
    /// <returns>A string representation of the person's current status.</returns>
    public override string ToString()
    {
        return $"Person {Id} from floor {OriginFloor} to floor {DestinationFloor}";
    }
}
