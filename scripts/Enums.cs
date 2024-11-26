namespace GodotSharpRR;

using System;

public enum DIRECTION
{
    NORTH = 0,
    EAST = 1,
    SOUTH = 2,
    WEST = 3,
}

[Flags]
public enum ROBOT
{
    RED = 0,
    GREEN = 1,
    BLUE = 2,
    YELLOW = 4,
    SILVER = 8, // currently is not used
}

public enum SHAPE
{
    VORTEX = -1,
    CIRCLE = 0,
    TRIANGLE = 1,
    SQUARE = 3,
    HEXAGON = 4,
}
