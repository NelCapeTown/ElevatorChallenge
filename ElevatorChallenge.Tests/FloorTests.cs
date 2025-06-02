using ElevatorChallenge.ElevatorClasses;
using ElevatorChallenge.Enums;

using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace ElevatorChallenge.Tests;

public class FloorTests
{
    private readonly Mock<ILogger<Floor>> _loggerMock;

    public FloorTests()
    {
        _loggerMock = new Mock<ILogger<Floor>>();
    }

    [Fact]
    public void Constructor_SetsProperties_Correctly()
    {
        var floor = new Floor(_loggerMock.Object, 5);
        floor.FloorNumber.ShouldBe(5);
        floor.PeopleWaitingUp.ShouldNotBeNull();
        floor.PeopleWaitingDown.ShouldNotBeNull();
        floor.PeopleWaitingUp.ShouldBeEmpty();
        floor.PeopleWaitingDown.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_Throws_WhenLoggerIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new Floor(null!, 1));
    }

    [Fact]
    public void AddWaitingPerson_AddsToUpList_WhenDestinationAbove()
    {
        var floor = new Floor(_loggerMock.Object, 1);
        var person = new Person(1, 3);

        floor.AddWaitingPerson(person);

        floor.PeopleWaitingUp.ShouldContain(person);
        floor.PeopleWaitingDown.ShouldBeEmpty();
    }

    [Fact]
    public void AddWaitingPerson_AddsToDownList_WhenDestinationBelow()
    {
        var floor = new Floor(_loggerMock.Object, 3);
        var person = new Person(3, 1);

        floor.AddWaitingPerson(person);

        floor.PeopleWaitingDown.ShouldContain(person);
        floor.PeopleWaitingUp.ShouldBeEmpty();
    }

    [Fact]
    public void AddWaitingPerson_DoesNotAdd_WhenDestinationIsSame()
    {
        var floor = new Floor(_loggerMock.Object, 2);
        var person = new Person(2, 2);

        floor.AddWaitingPerson(person);

        floor.PeopleWaitingUp.ShouldBeEmpty();
        floor.PeopleWaitingDown.ShouldBeEmpty();
    }

    [Fact]
    public void GetNextWaitingPerson_ReturnsAndRemovesFromUpList()
    {
        var floor = new Floor(_loggerMock.Object, 1);
        var person1 = new Person(1, 3);
        var person2 = new Person(1, 4);
        floor.AddWaitingPerson(person1);
        floor.AddWaitingPerson(person2);

        var next = floor.GetNextWaitingPerson(Direction.Up);

        next.ShouldBe(person1);
        floor.PeopleWaitingUp.ShouldNotContain(person1);
        floor.PeopleWaitingUp.ShouldContain(person2);
    }

    [Fact]
    public void GetNextWaitingPerson_ReturnsAndRemovesFromDownList()
    {
        var floor = new Floor(_loggerMock.Object, 3);
        var person1 = new Person(3, 1);
        var person2 = new Person(3, 0);
        floor.AddWaitingPerson(person1);
        floor.AddWaitingPerson(person2);

        var next = floor.GetNextWaitingPerson(Direction.Down);

        next.ShouldBe(person1);
        floor.PeopleWaitingDown.ShouldNotContain(person1);
        floor.PeopleWaitingDown.ShouldContain(person2);
    }

    [Fact]
    public void GetNextWaitingPerson_ReturnsNull_WhenNoOneWaiting()
    {
        var floor = new Floor(_loggerMock.Object, 2);

        floor.GetNextWaitingPerson(Direction.Up).ShouldBeNull();
        floor.GetNextWaitingPerson(Direction.Down).ShouldBeNull();
    }

    [Theory]
    [InlineData(Direction.Up, 2, 0)]
    [InlineData(Direction.Down, 0, 2)]
    public void WaitingCount_ReturnsCorrectCount(Direction direction, int upCount, int downCount)
    {
        var floor = new Floor(_loggerMock.Object, 2);
        for (int i = 0; i < upCount; i++)
            floor.AddWaitingPerson(new Person(2, 3 + i));
        for (int i = 0; i < downCount; i++)
            floor.AddWaitingPerson(new Person(2, 1 - i));

        floor.WaitingCount(Direction.Up).ShouldBe(upCount);
        floor.WaitingCount(Direction.Down).ShouldBe(downCount);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        var floor = new Floor(_loggerMock.Object, 1);
        floor.AddWaitingPerson(new Person(1, 2));
        floor.AddWaitingPerson(new Person(1, 3));
        floor.AddWaitingPerson(new Person(1, 0));

        var str = floor.ToString();
        str.ShouldContain("Floor 1");
        str.ShouldContain("Waiting Up: 2");
        str.ShouldContain("Waiting Down: 1");
    }
}