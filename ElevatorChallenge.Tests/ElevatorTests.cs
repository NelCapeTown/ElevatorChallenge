using ElevatorChallenge.ElevatorClasses;
using ElevatorChallenge.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace ElevatorChallenge.Tests;

public class ElevatorTests
{
    private readonly Mock<ILogger<Elevator>> _loggerMock;

    public ElevatorTests()
    {
        _loggerMock = new Mock<ILogger<Elevator>>();
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var elevator = new Elevator(_loggerMock.Object, startingFloor: 2, maxCapacity: 5);

        elevator.CurrentFloor.ShouldBe(2);
        elevator.MaxCapacity.ShouldBe(5);
        elevator.CurrentDirection.ShouldBe(Direction.Stopped);
        elevator.State.ShouldBe(ElevatorState.Stopped);
        elevator.Passengers.ShouldBeEmpty();
    }

    [Fact]
    public void AddPassenger_WhenNotFull_AddsPassenger()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 2);
        var person = new Person(1, 3);

        var result = elevator.AddPassenger(person);

        result.ShouldBeTrue();
        elevator.Passengers.ShouldContain(person);
    }

    [Fact]
    public void AddPassenger_WhenFull_ReturnsFalse()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 1);
        elevator.AddPassenger(new Person(1, 2));

        var result = elevator.AddPassenger(new Person(1, 3));

        result.ShouldBeFalse();
        elevator.Passengers.Count.ShouldBe(1);
    }

    [Fact]
    public void AddDestination_AddsFloorToDestinations()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);

        elevator.AddDestination(3, Direction.Up);

        elevator.ToString().ShouldContain("3");
    }

    [Fact]
    public void IsStopped_WhenStopped_ReturnsTrue()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);

        elevator.IsStopped().ShouldBeTrue();
    }

    [Fact]
    public void GetPickupRequestCountAtFloor_ReturnsCorrectCount()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);
        elevator.AddDestination(2, Direction.Up);
        elevator.AddDestination(2, Direction.Up);

        elevator.GetPickupRequestCountAtFloor(2).ShouldBe(1); // Only one unique pickup per floor/direction
    }

    [Fact]
    public void IsMovingTowards_ReturnsTrue_WhenMovingUpAndTargetAbove()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);
        elevator.CurrentDirection = Direction.Up;
        elevator.State = ElevatorState.Moving;
        elevator.CurrentFloor = 2;

        elevator.IsMovingTowards(4, Direction.Up).ShouldBeTrue();
    }

    [Fact]
    public void IsMovingTowards_ReturnsFalse_WhenMovingDownAndTargetAbove()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);
        elevator.CurrentDirection = Direction.Down;
        elevator.State = ElevatorState.Moving;
        elevator.CurrentFloor = 2;

        elevator.IsMovingTowards(4, Direction.Up).ShouldBeFalse();
    }

    [Fact]
    public void UnloadPassengers_RemovesPassengersAtCurrentFloor()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);
        var person1 = new Person(1, 2);
        var person2 = new Person(1, 3);
        elevator.Passengers.Add(person1);
        elevator.Passengers.Add(person2);
        elevator.CurrentFloor = 2;
        elevator.State = ElevatorState.DoorsOpen;

        elevator.UnloadPassengers();

        elevator.Passengers.ShouldNotContain(person1);
        elevator.Passengers.ShouldContain(person2);
    }

    [Fact]
    public void Step_DoesNotThrow_WhenIdle()
    {
        var elevator = new Elevator(_loggerMock.Object, 1, 5);
        Should.NotThrow(() => elevator.Step());
    }

    // Add more tests for edge cases, e.g. Step with destinations, AddDestination with duplicate, etc.
}