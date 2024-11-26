namespace GodotSharpRR;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public partial class Board
{
    public record Move(ROBOT Robot, int OldPosition, int NewPosition);

    public const int WIDTH = 16;
    public const int HEIGHT = 16;

    public Board()
        : this(WIDTH, HEIGHT)
    { }

    private Board(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.size = width * height;
        this.directionIncrement = new int[4];
        this.directionIncrement[(int)DIRECTION.NORTH] = -width;
        this.directionIncrement[(int)DIRECTION.EAST] = 1;
        this.directionIncrement[(int)DIRECTION.SOUTH] = width;
        this.directionIncrement[(int)DIRECTION.WEST] = -1;
        this.quadrants = new int[4];
        this.walls = new bool[4, this.width * this.height];
        this.goals = [];
        this.randomGoals = [];
        this.goal = null;
        this.robots = [];
    }

    private readonly int width;
    private readonly int height;
    private readonly int size;

    public readonly int[] directionIncrement;

    private readonly int[] quadrants;
    private readonly bool[,] walls; // direction, position

    private Dictionary<ROBOT, int> robots;

    private readonly List<Goal> goals;
    private readonly List<Goal> randomGoals;
    private Goal? goal;

    private readonly bool isFreeStyle = true;

    public bool IsSolved()
    {
        if (goal == null)
        {
            return false;
        }

        return goal.Robot == ANYROBOT
            ? this.robots.ContainsValue(goal.Position)
            : goal.Position == this.robots[goal.Robot];
    }

    public bool[,] GetWalls()
    {
        return this.walls;
    }

    public Move? MoveRobot(ROBOT robot, DIRECTION direction)
    {
        int currentPosition = this.robots[robot];
        int moveToPosition = currentPosition;
        int tempPosition = currentPosition;

        int counter = 0;

        while (true)
        {
            // same position check for walls
            if (this.walls[(int)direction, tempPosition])
            {
                break;
            }

            // move position and check for obstacles (robots)
            tempPosition += this.directionIncrement[(int)direction];
            if (this.robots.ContainsValue(tempPosition))
            {
                // new position is occupied from a robot
                break;
            }

            // if here then position is free
            moveToPosition = tempPosition;

            counter++;
            if ((counter > WIDTH && (direction == DIRECTION.EAST || direction == DIRECTION.WEST))
            || (counter > HEIGHT && (direction == DIRECTION.NORTH || direction == DIRECTION.SOUTH)))
            {
                return null;
            }
        }

        if (moveToPosition != currentPosition)
        {
            this.robots[robot] = moveToPosition;
            return new Move(robot, currentPosition, moveToPosition);
        }

        return null;
    }

    public void MoveRobotToPosition(ROBOT robot, int position)
    {
        if (!this.IsRobotPos(position))
        {
            this.robots[robot] = position;
        }
    }

    public Goal? GetGoal()
    {
        return this.goal;
    }

    public List<Goal> GetAllGoals()
    {
        return this.goals;
    }

    public (int, int) GetPositionAsXY(int position)
    {
        int x = position % this.width;
        int y = position / this.width;

        return (x, y);
    }

    public int GetRobotPosition(ROBOT robot)
    {
        return this.robots[robot];
    }

    public static Board CreateBoardRandom(int numRobots)
    {
        List<int> indexList = Enumerable.Range(0, 4).ToList();
        indexList.Shuffle();

        return CreateBoardQuadrants(
            indexList[0] + Random.Shared.Next(3 + 1) * 4,
            indexList[1] + Random.Shared.Next(3 + 1) * 4,
            indexList[2] + Random.Shared.Next(3 + 1) * 4,
            indexList[3] + Random.Shared.Next(3 + 1) * 4);
    }

    public static Board CreateBoardQuadrants(
        int quadrantNW,
        int quadrantNE,
        int quadrantSE,
        int quadrantSW)
    {
        Board b = new(WIDTH, HEIGHT);
        //add walls and goals
        b.AddQuadrant(quadrantNW, 0);
        b.AddQuadrant(quadrantNE, 1);
        b.AddQuadrant(quadrantSE, 2);
        b.AddQuadrant(quadrantSW, 3);
        b.AddOuterWalls();
        b.SetRobots();
        b.SetGoalRandom();

        return b;
    }

    #region robots

    private bool IsSolution01()
    {
        if (this.goal == null)
        {
            return false;
        }

        foreach (var robo in this.robots.Keys)
        {
            if (this.goal.Robot != robo && (int)this.goal.Robot != (int)ANYROBOT)
            {
                continue; // skip because it's not the goal robot
            }

            int oldRoboPos = this.robots[robo];

            if (this.goal.Position == oldRoboPos)
            {
                return true; // already on goal
            }

            int dir = -1;
            foreach (int dirIncr in this.directionIncrement)
            {
                ++dir;
                int newRoboPos = oldRoboPos;
                int prevRoboPos = oldRoboPos;
                // move the robot until it reaches a wall or another robot.
                // NOTE: we rely on the fact that all boards are surrounded
                // by outer walls. without the outer walls we would need
                // some additional boundary checking here.
                while (true)
                {
                    if (true == this.walls[dir, newRoboPos])
                    { // stopped by wall
                        if (this.goal.Position == newRoboPos)
                        {
                            return true; // one move to goal
                        }
                        break; // can't go on
                    }
                    if (true == this.IsRobotPos(newRoboPos))
                    { // stopped by robot
                        if (this.goal.Position == prevRoboPos)
                        {
                            return true; // one move to goal
                        }
                        // go on in this direction
                    }
                    prevRoboPos = newRoboPos;
                    newRoboPos += dirIncr;
                }
            }
        }

        return false;
    }

    private void SetRobots()
    {
        this.robots = [];
        if (this.isFreeStyle)
        {
            this.SetRobotsRandom();
        }
        else
        {
            //original board / made out of quadrants
            this.SetRobot(ROBOT.RED, 14 + 2 * this.width); //R
            this.SetRobot(ROBOT.GREEN, 1 + 2 * this.width); //G
            this.SetRobot(ROBOT.BLUE, 13 + 11 * this.width); //B
            this.SetRobot(ROBOT.YELLOW, 15 + 0 * this.width); //Y
            this.SetRobot(ROBOT.SILVER, 15 + 7 * this.width); //S
        }
    }

    public void SetRobotsRandom()
    {
        ROBOT[] RGBY = [ROBOT.RED, ROBOT.GREEN, ROBOT.BLUE, ROBOT.YELLOW];
        do
        {
            foreach (ROBOT robot in RGBY)
            {
                int position;
                do
                {
                    position = Random.Shared.Next(this.size);
                } while (false == this.SetRobot(robot, position));
            }
        } while (this.IsSolution01());
    }


    private bool SetRobot(ROBOT robot, int position)
    {
        if (robot < 0
            || !robot.IsSingleRobot()  // enum has a single set bit
            || position < 0
            || position >= this.size
            || this.IsObstacle(position)
            || this.robots.ContainsValue(position))
        {
            return false;
        }
        else
        {
            this.robots[robot] = position;
            return true;
        }
    }

    private bool IsRobotPos(int position)
    {
        return this.robots.ContainsValue(position);
    }

    #endregion

    #region quadrants

    private Board AddQuadrant(int qNum, int qPos)
    {
        this.quadrants[qPos] = qNum;
        Board quadrant = QUADRANTS[qNum];
        //qPos (quadrant target position): 0==NW, 1==NE, 2==SE, 3==SW
        int qX, qY;

        //add walls
        for (qY = 0; qY < quadrant.height / 2; ++qY)
        {
            for (qX = 0; qX < quadrant.width / 2; ++qX)
            {
                for (int dir = 0; dir < 4; ++dir)
                {
                    this.walls[(dir + qPos) & 3, this.TransformQuadrantPosition(qX, qY, qPos)] |=
                        quadrant.walls[dir, qX + qY * quadrant.width];
                }
            }

            this.walls[((int)DIRECTION.WEST + qPos) & 3, this.TransformQuadrantPosition(qX, qY, qPos)] |=
                quadrant.walls[(int)DIRECTION.EAST, qX - 1 + qY * quadrant.width];
        }

        for (qX = 0; qX < quadrant.width / 2; ++qX)
        {
            this.walls[((int)DIRECTION.NORTH + qPos) & 3, this.TransformQuadrantPosition(qX, qY, qPos)] |=
                quadrant.walls[(int)DIRECTION.SOUTH, qX + (qY - 1) * quadrant.width];
        }

        //add goals
        foreach (Goal g in quadrant.goals)
        {
            this.AddGoal(this.TransformQuadrantX(g.X, g.Y, qPos), this.TransformQuadrantY(g.X, g.Y, qPos), g.Robot, g.Shape);
        }
        return this;
    }

    private int TransformQuadrantPosition(int qX, int qY, int qPos)
    {
        return (this.TransformQuadrantX(qX, qY, qPos) + this.TransformQuadrantY(qX, qY, qPos) * this.width);
    }

    private int TransformQuadrantX(int qX, int qY, int qPos)
    {
        //qPos (quadrant target position): 0==NW, 1==NE, 2==SE, 3==SW
        var resultX = qPos switch
        {
            1 => this.width - 1 - qY,
            2 => this.width - 1 - qX,
            3 => qY,
            _ => qX,
        };
        return resultX;
    }
    private int TransformQuadrantY(int qX, int qY, int qPos)
    {
        //qPos (quadrant target position): 0==NW, 1==NE, 2==SE, 3==SW
        var resultY = qPos switch
        {
            1 => qX,
            2 => this.height - 1 - qY,
            3 => this.height - 1 - qX,
            _ => qY,
        };
        return resultY;
    }

    #endregion

    #region walls

    private Board AddOuterWalls()
    {
        return this.SetOuterWalls(true);
    }

    private Board SetOuterWalls(bool value)
    {
        for (int x = 0; x < this.width; ++x)
        {
            this.SetWall(x, 0, DIRECTION.NORTH, value);
            this.SetWall(x, this.height - 1, DIRECTION.SOUTH, value);
        }

        for (int y = 0; y < this.height; ++y)
        {
            this.SetWall(0, y, DIRECTION.WEST, value);
            this.SetWall(this.width - 1, y, DIRECTION.EAST, value);
        }
        return this;
    }

    private Board AddWall(int x, int y, string str)
    {
        this.SetWalls(x, y, str, true);
        return this;
    }

    private void SetWalls(int x, int y, string str, bool value)
    {
        if (str.Contains('N'))
        {
            this.SetWall(x, y, DIRECTION.NORTH, value);
            this.SetWall(x, y - 1, DIRECTION.SOUTH, value);
        }
        if (str.Contains('E'))
        {
            this.SetWall(x, y, DIRECTION.EAST, value);
            this.SetWall(x + 1, y, DIRECTION.WEST, value);
        }
        if (str.Contains('S'))
        {
            this.SetWall(x, y, DIRECTION.SOUTH, value);
            this.SetWall(x, y + 1, DIRECTION.NORTH, value);
        }
        if (str.Contains('W'))
        {
            this.SetWall(x, y, DIRECTION.WEST, value);
            this.SetWall(x - 1, y, DIRECTION.EAST, value);
        }
    }

    private void SetWall(int x, int y, DIRECTION direction, bool value)
    {
        if ((x >= 0) && (x < this.width) && (y >= 0) && (y < this.height))
        {
            this.walls[(int)direction, x + y * this.width] = value;
        }
    }

    private bool IsWall(int position, int direction)
    {
        return this.walls[direction, position];
    }

    public bool IsObstacle(int position)
    {
        return this.IsWall(position, (int)DIRECTION.NORTH)
            && this.IsWall(position, (int)DIRECTION.EAST)
            && this.IsWall(position, (int)DIRECTION.SOUTH)
            && this.IsWall(position, (int)DIRECTION.WEST);
    }

    #endregion

    #region goals

    public void SetGoalRandom()
    {
        if (this.goals.Count == 0)
        {
            this.goal = null;
            return;
        }

        if (this.randomGoals.Count == 0)
        {
            this.randomGoals.AddRange(this.goals);
            this.randomGoals.Shuffle();
        }

        this.goal = this.randomGoals[0];
        this.randomGoals.RemoveAt(0);

        if (this.IsSolution01() && (this.randomGoals.Count > 0))
        {
            //the resulting board configuration has a solution of 0 or 1 move
            //and there are some other goals available in list randomGoals.
            Goal lastResortGoal = this.goal;
            this.SetGoalRandom();
            this.randomGoals.Add(lastResortGoal);
        }
    }

    private Board AddGoal(int x, int y, ROBOT robot, SHAPE shape)
    {
        Goal g = new(x, y, robot, shape);
        this.goals.Add(g);
        if (null == this.goal)
        {
            this.goal = g;
        }

        return this;
    }

    #endregion

    private const ROBOT ANYROBOT = ROBOT.RED | ROBOT.GREEN | ROBOT.BLUE | ROBOT.YELLOW;

    private readonly static Board[] QUADRANTS =
    [
        new Board(WIDTH, HEIGHT) //1A
            .AddWall(1, 0, "E")
            .AddWall(4, 1, "NW").AddGoal(4, 1, ROBOT.RED, SHAPE.CIRCLE)      //R
            .AddWall(1, 2, "NE").AddGoal(1, 2, ROBOT.GREEN, SHAPE.TRIANGLE)    //G
            .AddWall(6, 3, "SE").AddGoal(6, 3, ROBOT.YELLOW, SHAPE.HEXAGON)     //Y
            .AddWall(0, 5, "S")
            .AddWall(3, 6, "SW").AddGoal(3, 6, ROBOT.BLUE, SHAPE.SQUARE)      //B
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //2A
            .AddWall(3, 0, "E")
            .AddWall(5, 1, "SE").AddGoal(5, 1, ROBOT.GREEN, SHAPE.HEXAGON)     //G
            .AddWall(1, 2, "SW").AddGoal(1, 2, ROBOT.RED, SHAPE.SQUARE)      //R
            .AddWall(0, 3, "S")
            .AddWall(6, 4, "NW").AddGoal(6, 4, ROBOT.YELLOW, SHAPE.CIRCLE)      //Y
            .AddWall(2, 6, "NE").AddGoal(2, 6, ROBOT.BLUE, SHAPE.TRIANGLE)    //B
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //3A
            .AddWall(3, 0, "E")
            .AddWall(5, 2, "SE").AddGoal(5, 2, ROBOT.BLUE, SHAPE.HEXAGON)     //B
            .AddWall(0, 4, "S")
            .AddWall(2, 4, "NE").AddGoal(2, 4, ROBOT.GREEN, SHAPE.CIRCLE)      //G
            .AddWall(7, 5, "SW").AddGoal(7, 5, ROBOT.RED, SHAPE.TRIANGLE)    //R
            .AddWall(1, 6, "NW").AddGoal(1, 6, ROBOT.YELLOW, SHAPE.SQUARE)      //Y
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //4A
            .AddWall(3, 0, "E")
            .AddWall(6, 1, "SW").AddGoal(6, 1, ROBOT.BLUE, SHAPE.CIRCLE)      //B
            .AddWall(1, 3, "NE").AddGoal(1, 3, ROBOT.YELLOW, SHAPE.TRIANGLE)    //Y
            .AddWall(5, 4, "NW").AddGoal(5, 4, ROBOT.GREEN, SHAPE.SQUARE)      //G
            .AddWall(2, 5, "SE").AddGoal(2, 5, ROBOT.RED, SHAPE.HEXAGON)     //R
            .AddWall(7, 5, "SE").AddGoal(7, 5, ANYROBOT, SHAPE.VORTEX)     //W*
            .AddWall(0, 6, "S")
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //1B
            .AddWall(4, 0, "E")
            .AddWall(6, 1, "SE").AddGoal(6, 1, ROBOT.YELLOW, SHAPE.HEXAGON)     //Y
            .AddWall(1, 2, "NW").AddGoal(1, 2, ROBOT.GREEN, SHAPE.TRIANGLE)    //G
            .AddWall(0, 5, "S")
            .AddWall(6, 5, "NE").AddGoal(6, 5, ROBOT.BLUE, SHAPE.SQUARE)      //B
            .AddWall(3, 6, "SW").AddGoal(3, 6, ROBOT.RED, SHAPE.CIRCLE)      //R
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //2B
            .AddWall(4, 0, "E")
            .AddWall(2, 1, "NW").AddGoal(2, 1, ROBOT.YELLOW, SHAPE.CIRCLE)      //Y
            .AddWall(6, 3, "SW").AddGoal(6, 3, ROBOT.BLUE, SHAPE.TRIANGLE)    //B
            .AddWall(0, 4, "S")
            .AddWall(4, 5, "NE").AddGoal(4, 5, ROBOT.RED, SHAPE.SQUARE)      //R
            .AddWall(1, 6, "SE").AddGoal(1, 6, ROBOT.GREEN, SHAPE.HEXAGON)     //G
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //3B
            .AddWall(3, 0, "E")
            .AddWall(1, 1, "SW").AddGoal(1, 1, ROBOT.RED, SHAPE.TRIANGLE)    //R
            .AddWall(6, 2, "NE").AddGoal(6, 2, ROBOT.GREEN, SHAPE.CIRCLE)      //G
            .AddWall(2, 4, "SE").AddGoal(2, 4, ROBOT.BLUE, SHAPE.HEXAGON)     //B
            .AddWall(0, 5, "S")
            .AddWall(7, 5, "NW").AddGoal(7, 5, ROBOT.YELLOW, SHAPE.SQUARE)      //Y
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //4B
            .AddWall(4, 0, "E")
            .AddWall(2, 1, "SE").AddGoal(2, 1, ROBOT.RED, SHAPE.HEXAGON)     //R
            .AddWall(1, 3, "SW").AddGoal(1, 3, ROBOT.GREEN, SHAPE.SQUARE)      //G
            .AddWall(0, 4, "S")
            .AddWall(6, 4, "NW").AddGoal(6, 4, ROBOT.YELLOW, SHAPE.TRIANGLE)    //Y
            .AddWall(5, 6, "NE").AddGoal(5, 6, ROBOT.BLUE, SHAPE.CIRCLE)      //B
            .AddWall(3, 7, "SE").AddGoal(3, 7, ANYROBOT, SHAPE.VORTEX)     //W*
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //1C
            .AddWall(1, 0, "E")
            .AddWall(3, 1, "NW").AddGoal(3, 1, ROBOT.GREEN, SHAPE.TRIANGLE)    //G
            .AddWall(6, 3, "SE").AddGoal(6, 3, ROBOT.YELLOW, SHAPE.HEXAGON)     //Y
            .AddWall(1, 4, "SW").AddGoal(1, 4, ROBOT.RED, SHAPE.CIRCLE)      //R
            .AddWall(0, 6, "S")
            .AddWall(4, 6, "NE").AddGoal(4, 6, ROBOT.BLUE, SHAPE.SQUARE)      //B
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //2C
            .AddWall(5, 0, "E")
            .AddWall(3, 2, "NW").AddGoal(3, 2, ROBOT.YELLOW, SHAPE.CIRCLE)      //Y
            .AddWall(0, 3, "S")
            .AddWall(5, 3, "SW").AddGoal(5, 3, ROBOT.BLUE, SHAPE.TRIANGLE)    //B
            .AddWall(2, 4, "NE").AddGoal(2, 4, ROBOT.RED, SHAPE.SQUARE)      //R
            .AddWall(4, 5, "SE").AddGoal(4, 5, ROBOT.GREEN, SHAPE.HEXAGON)     //G
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //3C
            .AddWall(1, 0, "E")
            .AddWall(4, 1, "NE").AddGoal(4, 1, ROBOT.GREEN, SHAPE.CIRCLE)      //G
            .AddWall(1, 3, "SW").AddGoal(1, 3, ROBOT.RED, SHAPE.TRIANGLE)    //R
            .AddWall(0, 5, "S")
            .AddWall(5, 5, "NW").AddGoal(5, 5, ROBOT.YELLOW, SHAPE.SQUARE)      //Y
            .AddWall(3, 6, "SE").AddGoal(3, 6, ROBOT.BLUE, SHAPE.HEXAGON)     //B
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //4C
            .AddWall(2, 0, "E")
            .AddWall(5, 1, "SW").AddGoal(5, 1, ROBOT.BLUE, SHAPE.CIRCLE)      //B
            .AddWall(7, 2, "SE").AddGoal(7, 2, ANYROBOT, SHAPE.VORTEX)     //W*
            .AddWall(0, 3, "S")
            .AddWall(3, 4, "SE").AddGoal(3, 4, ROBOT.RED, SHAPE.HEXAGON)     //R
            .AddWall(6, 5, "NW").AddGoal(6, 5, ROBOT.GREEN, SHAPE.SQUARE)      //G
            .AddWall(1, 6, "NE").AddGoal(1, 6, ROBOT.YELLOW, SHAPE.TRIANGLE)    //Y
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //1D
            .AddWall(5, 0, "E")
            .AddWall(1, 3, "NW").AddGoal(1, 3, ROBOT.RED, SHAPE.CIRCLE)      //R
            .AddWall(6, 4, "SE").AddGoal(6, 4, ROBOT.YELLOW, SHAPE.HEXAGON)     //Y
            .AddWall(0, 5, "S")
            .AddWall(2, 6, "NE").AddGoal(2, 6, ROBOT.GREEN, SHAPE.TRIANGLE)    //G
            .AddWall(3, 6, "SW").AddGoal(3, 6, ROBOT.BLUE, SHAPE.SQUARE)      //B
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //2D
            .AddWall(2, 0, "E")
            .AddWall(5, 2, "SE").AddGoal(5, 2, ROBOT.GREEN, SHAPE.HEXAGON)     //G
            .AddWall(6, 2, "NW").AddGoal(6, 2, ROBOT.YELLOW, SHAPE.CIRCLE)      //Y
            .AddWall(1, 5, "SW").AddGoal(1, 5, ROBOT.RED, SHAPE.SQUARE)      //R
            .AddWall(0, 6, "S")
            .AddWall(4, 7, "NE").AddGoal(4, 7, ROBOT.BLUE, SHAPE.TRIANGLE)    //B
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //3D
            .AddWall(4, 0, "E")
            .AddWall(0, 2, "S")
            .AddWall(6, 2, "SE").AddGoal(6, 2, ROBOT.BLUE, SHAPE.HEXAGON)     //B
            .AddWall(2, 4, "NE").AddGoal(2, 4, ROBOT.GREEN, SHAPE.CIRCLE)      //G
            .AddWall(3, 4, "SW").AddGoal(3, 4, ROBOT.RED, SHAPE.TRIANGLE)    //R
            .AddWall(5, 6, "NW").AddGoal(5, 6, ROBOT.YELLOW, SHAPE.SQUARE)      //Y
            .AddWall(7, 7, "NESW"),
        new Board(WIDTH, HEIGHT) //4D
            .AddWall(4, 0, "E")
            .AddWall(6, 2, "NW").AddGoal(6, 2, ROBOT.YELLOW, SHAPE.TRIANGLE)    //Y
            .AddWall(2, 3, "NE").AddGoal(2, 3, ROBOT.BLUE, SHAPE.CIRCLE)      //B
            .AddWall(3, 3, "SW").AddGoal(3, 3, ROBOT.GREEN, SHAPE.SQUARE)      //G
            .AddWall(1, 5, "SE").AddGoal(1, 5, ROBOT.RED, SHAPE.HEXAGON)     //R
            .AddWall(0, 6, "S")
            .AddWall(5, 7, "SE").AddGoal(5, 7, ANYROBOT, SHAPE.VORTEX)     //W*
            .AddWall(7, 7, "NESW"),
    ];
}
