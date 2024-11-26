namespace GodotSharpRR;

using System;
using System.Collections.Generic;
using System.Numerics;

public static class EnumExtensions
{
    public static bool IsSingleRobot(this ROBOT robot)
    {
        int numberOfSetBits = BitOperations.PopCount((uint)robot);
        return numberOfSetBits <= 1;
    }
}
