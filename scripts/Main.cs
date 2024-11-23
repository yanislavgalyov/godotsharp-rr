using System.Runtime.CompilerServices;

namespace GodotSharpRR;

using Godot;

using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
    [Signal]
    public delegate void BoardSolvedEventHandler();

    private PackedScene vortextGoalScene = ResourceLoader.Load<PackedScene>("res://scenes/vortex_goal.tscn");
    private PackedScene circleGoalScene = ResourceLoader.Load<PackedScene>("res://scenes/circle_goal.tscn");
    private PackedScene triangleGoalScene = ResourceLoader.Load<PackedScene>("res://scenes/triangle_goal.tscn");
    private PackedScene squareGoalScene = ResourceLoader.Load<PackedScene>("res://scenes/square_goal.tscn");
    private PackedScene hexagonGoalScene = ResourceLoader.Load<PackedScene>("res://scenes/hexagon_goal.tscn");

    private GodotObject countdownTimer = null!;

    private Board board = null!;

    private Sprite2D redRobot = null!;
    private Sprite2D greenRobot = null!;
    private Sprite2D blueRobot = null!;
    private Sprite2D yellowRobot = null!;

    private Node2D wallsContainer = null!;
    private Node2D goalsContainer = null!;

    private TileMapLayer tileMapLayer = null!;

    private Label totalBoards = null!;

    private Label totalMoves = null!;

    private ROBOT? selectedRobot = ROBOT.RED;

    private int solvedBoardsCount;
    private int totalMovesCount;

    private bool isFrozen = false;


    public override void _Ready()
    {
        this.tileMapLayer = this.GetNode<TileMapLayer>("TileMapLayer");

        this.goalsContainer = this.GetNode<Node2D>("Goals");
        this.wallsContainer = this.GetNode<Node2D>("Walls");

        this.redRobot = this.GetNode<Sprite2D>("Robots/Red");
        this.greenRobot = this.GetNode<Sprite2D>("Robots/Green");
        this.blueRobot = this.GetNode<Sprite2D>("Robots/Blue");
        this.yellowRobot = this.GetNode<Sprite2D>("Robots/Yellow");

        this.totalBoards = this.GetNode<Label>("TotalBoards");
        this.totalMoves = this.GetNode<Label>("TotalMoves");

        this.countdownTimer = this.GetNode("CountdownTimer");

        this.countdownTimer.Connect("countdown_timer_done", Callable.From(this.CountdownTimerDoneHandler));

        this.SetupOnce();
        this.SetupBoard();

        this.BoardSolved += this.BoardSolvedHandler;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Up"))
        {
            MoveRobot(DIRECTION.NORTH);
        }
        else if (Input.IsActionJustPressed("Right"))
        {
            MoveRobot(DIRECTION.EAST);
        }
        else if (Input.IsActionJustPressed("Down"))
        {
            MoveRobot(DIRECTION.SOUTH);
        }
        else if (Input.IsActionJustPressed("Left"))
        {
            MoveRobot(DIRECTION.WEST);
        }
        else if (Input.IsActionJustPressed("Reset"))
        {
            // todo: reset board vs reset 2 min
            GetTree().ReloadCurrentScene();
        }

        base._PhysicsProcess(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Red"))
        {
            this.selectedRobot = ROBOT.RED;
        }
        else if (@event.IsActionPressed("Green"))
        {
            this.selectedRobot = ROBOT.GREEN;
        }
        else if (@event.IsActionPressed("Blue"))
        {
            this.selectedRobot = ROBOT.BLUE;
        }
        else if (@event.IsActionPressed("Yellow"))
        {
            this.selectedRobot = ROBOT.YELLOW;
        }
        else if (@event.IsActionPressed("DebugSolve"))
        {
            EmitSignal(SignalName.BoardSolved);
        }
        // todo: undo

        base._Input(@event);
    }

    private void SetupOnce()
    {
        this.solvedBoardsCount = 0;
        this.totalMovesCount = 0;
    }

    private void SetupBoard()
    {
        foreach (Node child in this.wallsContainer.GetChildren())
        {
            this.wallsContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (Node child in this.goalsContainer.GetChildren())
        {
            this.goalsContainer.RemoveChild(child);
            child.QueueFree();
        }

        this.totalBoards.Text = $"Boards: {this.solvedBoardsCount}";
        this.totalMoves.Text = $"Moves: {this.totalMovesCount}";

        board = Board.CreateBoardRandom(4);

        (Goal? currentGoal, List<Goal> goals) = this.board.GetGoals();

        if (currentGoal == null)
        {
            GD.PrintErr("Missing GOAL");
        }

        foreach (Goal goal in goals)
        {
            if (goal == currentGoal)
            {
                Node? currentGoalScene = this.InstantiateGoalScene(goal.Shape);
                this.SetGoal(currentGoalScene as Sprite2D, 7, 7, goal.Robot);
                goalsContainer.AddChild(currentGoalScene);
            }

            Node? goalScene = this.InstantiateGoalScene(goal.Shape);

            if (goalScene != null)
            {
                this.SetGoal(goalScene as Sprite2D, goal.X, goal.Y, goal.Robot);
                goalsContainer.AddChild(goalScene);
            }
        }

        this.SetRobotPosition(ROBOT.RED);
        this.SetRobotPosition(ROBOT.GREEN);
        this.SetRobotPosition(ROBOT.BLUE);
        this.SetRobotPosition(ROBOT.YELLOW);

        this.selectedRobot = currentGoal?.Robot ?? ROBOT.RED;

        var walls = board.GetWalls();

        for (int dir = 0; dir < walls.GetLength(0); ++dir)
        {
            for (int pos = 0; pos < walls.GetLength(1); ++pos)
            {
                if (walls[dir, pos])
                {
                    this.DrawWall((DIRECTION)dir, pos);
                }
            }
        }
    }

    private void MoveRobot(DIRECTION direction)
    {
        if (this.isFrozen)
        {
            return;
        }

        _ = this.board.MoveRobot(this.selectedRobot!.Value, direction);
        this.SetRobotPosition(this.selectedRobot!.Value);

        this.totalMovesCount++;
        this.totalMoves.Text = $"Moves: {this.totalMovesCount}";

        if (this.board.IsSolved())
        {
            EmitSignal(SignalName.BoardSolved);
        }
    }

    private void BoardSolvedHandler()
    {
        this.solvedBoardsCount++;

        this.SetupBoard();
    }

    private void CountdownTimerDoneHandler()
    {
        this.isFrozen = true;
        // todo: stop game, show result
        // this.countdownTimer.Call("reset");
    }

    private Node? InstantiateGoalScene(SHAPE shape)
    {
        Node? scene = null;

        switch (shape)
        {
            case SHAPE.VORTEX:
                scene = vortextGoalScene.Instantiate();
                break;
            case SHAPE.CIRCLE:
                scene = circleGoalScene.Instantiate();
                break;
            case SHAPE.TRIANGLE:
                scene = triangleGoalScene.Instantiate();
                break;
            case SHAPE.SQUARE:
                scene = squareGoalScene.Instantiate();
                break;
            case SHAPE.HEXAGON:
                scene = hexagonGoalScene.Instantiate();
                break;
        }

        return scene;
    }

    private void DrawWall(DIRECTION direction, int position)
    {
        Vector2I tileSize = this.tileMapLayer.TileSet.TileSize;
        int halfWidth = tileSize.X / 2;
        int halfHeight = tileSize.Y / 2;

        (int x, int y) = this.board.GetPositionAsXY(position);
        Vector2 v2 = this.tileMapLayer.MapToLocal(new Vector2I(x, y));

        Vector2? start = null;
        Vector2? end = null;

        switch (direction)
        {
            case DIRECTION.NORTH:
                start = new Vector2(v2.X - halfWidth, v2.Y - halfHeight);
                end = new Vector2(v2.X + halfWidth, v2.Y - halfHeight);
                break;
            case DIRECTION.EAST:
                start = new Vector2(v2.X + halfWidth, v2.Y - halfHeight);
                end = new Vector2(v2.X + halfWidth, v2.Y + halfHeight);
                break;
            case DIRECTION.SOUTH:
                start = new Vector2(v2.X + halfWidth, v2.Y + halfHeight);
                end = new Vector2(v2.X - halfWidth, v2.Y + halfHeight);
                break;
            case DIRECTION.WEST:
                start = new Vector2(v2.X - halfWidth, v2.Y + halfHeight);
                end = new Vector2(v2.X - halfWidth, v2.Y - halfHeight);
                break;
        }

        if (start.HasValue && end.HasValue)
        {
            Line2D line = new();
            line.AddPoint(start.Value);
            line.AddPoint(end.Value);
            line.Width = 1f;
            line.DefaultColor = new Godot.Color(1, 1, 1);
            wallsContainer.AddChild(line);
        }
    }

    private void SetRobotPosition(ROBOT robot)
    {
        Sprite2D robotSprite = this.GetRobotSprite(robot);
        int robotPosition = this.board.GetRobotPosition(robot);
        (int robotX, int robotY) = this.board.GetPositionAsXY(robotPosition);
        robotSprite.Position = this.tileMapLayer.MapToLocal(new Vector2I(robotX, robotY));
    }

    private Sprite2D GetRobotSprite(ROBOT robot) => robot switch
    {
        ROBOT.RED => this.redRobot,
        ROBOT.GREEN => this.greenRobot,
        ROBOT.BLUE => this.blueRobot,
        ROBOT.YELLOW => this.yellowRobot,
        _ => throw new Exception("Unknown payload type.")
    };

    private void SetGoal(Sprite2D? sprite, int x, int y, ROBOT robot)
    {
        if (sprite != null)
        {
            sprite.Position = this.tileMapLayer.MapToLocal(new Vector2I(x, y));

            switch (robot)
            {
                case ROBOT.RED:
                    sprite.Modulate = new Godot.Color(1, 0, 0);
                    break;
                case ROBOT.GREEN:
                    sprite.Modulate = new Godot.Color(0, 1, 0);
                    break;
                case ROBOT.BLUE:
                    sprite.Modulate = new Godot.Color(0, 0, 1);
                    break;
                case ROBOT.YELLOW:
                    sprite.Modulate = new Godot.Color(1, 1, 0);
                    break;
            }
        }
    }
}
