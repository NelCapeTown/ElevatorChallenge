using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.Enums;

/// <summary>
/// Stores the direction of the elevator movement, or if it is stopped.
/// </summary>
public enum Direction
{
    /// <summary>
    /// The elevator is moving upward.
    /// </summary>
    Up,

    /// <summary>
    /// The elevator is moving downward.
    /// </summary>
    Down,

    /// <summary>
    /// The elevator is not moving.
    /// </summary>
    Stopped
}

/// <summary>
/// Stores the operational state of the elevator during operation.
/// </summary>
public enum ElevatorState
{
    /// <summary>
    /// The elevator is currently moving in a direction (up or down).
    /// </summary>
    Moving,

    /// <summary>
    /// The elevator is not moving but is operational and the doors are closed.
    /// </summary>
    Stopped,

    /// <summary>
    /// The elevator is stopped and the doors are open - probably loading or unloading passengers.
    /// </summary>
    DoorsOpen,

    /// <summary>
    /// This state can be used when the elevator is not operational.
    /// </summary>
    OutOfService
}
