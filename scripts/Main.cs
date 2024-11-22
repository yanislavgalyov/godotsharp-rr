namespace GodotSharpRR;

using Godot;

using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Main : Node2D
{
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

    private ROBOT? selectedRobot = ROBOT.RED;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.tileMapLayer = this.GetNode<TileMapLayer>("TileMapLayer");

        this.goalsContainer = this.GetNode<Node2D>("Goals");
        this.wallsContainer = this.GetNode<Node2D>("Walls");

        this.redRobot = this.GetNode<Sprite2D>("Robots/Red");
        this.greenRobot = this.GetNode<Sprite2D>("Robots/Green");
        this.blueRobot = this.GetNode<Sprite2D>("Robots/Blue");
        this.yellowRobot = this.GetNode<Sprite2D>("Robots/Yellow");

        this.countdownTimer = this.GetNode("CountdownTimer") as GodotObject;

        this.countdownTimer.Connect("countdown_timer_done", Callable.From(this.CountdownTimerDoneHandler));

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
                this.SetGoalPosition(currentGoalScene as Sprite2D, 7, 7);
                this.SetGoalModulate(currentGoalScene as Sprite2D, goal.Robot);
                goalsContainer.AddChild(currentGoalScene);
            }

            Node? goalScene = this.InstantiateGoalScene(goal.Shape);

            if (goalScene != null)
            {
                this.SetGoalPosition(goalScene as Sprite2D, goal.X, goal.Y);
                this.SetGoalModulate(goalScene as Sprite2D, goal.Robot);
                goalsContainer.AddChild(goalScene);
            }
        }

        this.SetRobotPosition(ROBOT.RED);
        this.SetRobotPosition(ROBOT.GREEN);
        this.SetRobotPosition(ROBOT.BLUE);
        this.SetRobotPosition(ROBOT.YELLOW);

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

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Up"))
        {
            this.board.MoveRobot(this.selectedRobot!.Value, DIRECTION.NORTH);
            this.SetRobotPosition(this.selectedRobot!.Value);
        }
        else if (Input.IsActionJustPressed("Right"))
        {
            this.board.MoveRobot(this.selectedRobot!.Value, DIRECTION.EAST);
            this.SetRobotPosition(this.selectedRobot!.Value);
        }
        else if (Input.IsActionJustPressed("Down"))
        {
            this.board.MoveRobot(this.selectedRobot!.Value, DIRECTION.SOUTH);
            this.SetRobotPosition(this.selectedRobot!.Value);
        }
        else if (Input.IsActionJustPressed("Left"))
        {
            this.board.MoveRobot(this.selectedRobot!.Value, DIRECTION.WEST);
            this.SetRobotPosition(this.selectedRobot!.Value);
        }
        else if (Input.IsActionJustPressed("Reset"))
        {
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
        if (@event.IsActionPressed("Green"))
        {
            this.selectedRobot = ROBOT.GREEN;
        }
        if (@event.IsActionPressed("Blue"))
        {
            this.selectedRobot = ROBOT.BLUE;
        }
        if (@event.IsActionPressed("Yellow"))
        {
            this.selectedRobot = ROBOT.YELLOW;
        }

        base._Input(@event);
    }

    private void CountdownTimerDoneHandler()
    {
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

    private void SetGoalPosition(Sprite2D? sprite, int x, int y)
    {
        if (sprite != null)
        {
            sprite.Position = this.tileMapLayer.MapToLocal(new Vector2I(x, y));
        }
    }

    private void SetGoalModulate(Sprite2D? sprite, ROBOT robot)
    {
        if (sprite != null)
        {
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
