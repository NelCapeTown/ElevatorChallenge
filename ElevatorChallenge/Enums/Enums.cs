using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.Enums;

public enum Direction
{
    Up,
    Down,
    Stopped
}

/// <summary>
/// 
/// </summary>
public enum ElevatorState
{
    Moving,
    Stopped,
    DoorsOpen,
    OutOfService // This state can be used when the elevator is not operational - 
}
internal class Enums
{
}
