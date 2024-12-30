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

    private PackedScene redRobotScene = ResourceLoader.Load<PackedScene>("res://scenes/red_robot.tscn");
    private PackedScene greenRobotScene = ResourceLoader.Load<PackedScene>("res://scenes/green_robot.tscn");
    private PackedScene blueRobotScene = ResourceLoader.Load<PackedScene>("res://scenes/blue_robot.tscn");
    private PackedScene yellowRobotScene = ResourceLoader.Load<PackedScene>("res://scenes/yellow_robot.tscn");

    private GodotObject countdownTimer = null!;

    private Node sceneCoordinator = null!;

    private Node globalRR = null!;

    private Board board = null!;

    private Dictionary<ROBOT, Sprite2D> robotSprites = null!;

    private Node goalsContainer = null!;
    private Node finalGoalsContainer = null!;
    private Node robotsCotainer = null!;
    private Node wallsContainer = null!;

    private TileMapLayer tileMapLayer = null!;

    private Label totalBoardsLabel = null!;
    private Label totalMovesLabel = null!;
    private Label timesUpLabel = null!;

    private ROBOT? selectedRobot;

    private int solvedBoardsCount;
    private int totalMovesCount;

    private Stack<Board.Move> currentBoardMoves = null!;

    private bool isFrozen = false;

    private static Color White = new(1, 1, 1, .7f);
    private static Color Transparent = new(1, 1, 1, 0);

    public override void _Ready()
    {
        this.sceneCoordinator = this.GetNode("/root/SceneCoordinator");
        this.globalRR = this.GetNode("/root/GlobalRR");

        this.tileMapLayer = this.GetNode<TileMapLayer>("TileMapLayer");

        this.goalsContainer = this.GetNode<Node>("Goals");
        this.finalGoalsContainer = this.GetNode<Node>("FinalGoals");
        this.robotsCotainer = this.GetNode<Node>("Robots");
        this.wallsContainer = this.GetNode<Node>("Walls");

        this.robotSprites = new Dictionary<ROBOT, Sprite2D>
        {
            { ROBOT.RED, this.GetNode<Sprite2D>("Robots/Red")},
            { ROBOT.GREEN, this.GetNode<Sprite2D>("Robots/Green")},
            { ROBOT.BLUE, this.GetNode<Sprite2D>("Robots/Blue")},
            { ROBOT.YELLOW, this.GetNode<Sprite2D>("Robots/Yellow")},
        };

        this.totalBoardsLabel = this.GetNode<Label>("TotalBoardsLabel");
        this.totalMovesLabel = this.GetNode<Label>("TotalMovesLabel");
        this.timesUpLabel = this.GetNode<Label>("TimesUpLabel");

        this.countdownTimer = this.GetNode("CountdownTimer");

        this.countdownTimer.Connect(
            "countdown_timer_done",
            Callable.From(this.CountdownTimerDoneHandler));

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
        else if (Input.IsActionJustPressed("Undo"))
        {
            this.UndoMoveRobot();
        }
        else if (Input.IsActionJustPressed("Reset"))
        {
            this.ResetBoard();
        }

        base._PhysicsProcess(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Red"))
        {
            this.SetSelectedRobot(ROBOT.RED);
        }
        else if (@event.IsActionPressed("Green"))
        {
            this.SetSelectedRobot(ROBOT.GREEN);
        }
        else if (@event.IsActionPressed("Blue"))
        {
            this.SetSelectedRobot(ROBOT.BLUE);
        }
        else if (@event.IsActionPressed("Yellow"))
        {
            this.SetSelectedRobot(ROBOT.YELLOW);
        }
        else if (@event.IsActionPressed("Exit"))
        {
            this.sceneCoordinator.CallDeferred(
                "append_scene",
                "res://scenes/main_menu.tscn");
        }
        else if (@event.IsActionPressed("StartMode"))
        {
            this.sceneCoordinator.CallDeferred(
                "append_scene",
                "res://scenes/main.tscn");
        }
        else if (@event.IsActionPressed("DebugSolve") && OS.IsDebugBuild())
        {
            EmitSignal(SignalName.BoardSolved);
        }

        base._Input(@event);
    }

    private void SetupOnce()
    {
        this.solvedBoardsCount = 0;
        this.totalMovesCount = 0;
    }

    private void SetupBoard()
    {
        #region clear phrase

        this.currentBoardMoves = new Stack<Board.Move>();

        // robots are kept for each board instead of recreated
        foreach (var item in this.robotSprites)
        {
            this.SetRobotShaderParam(item.Key, Transparent);
        }

        foreach (Node child in this.goalsContainer.GetChildren())
        {
            this.goalsContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (Node child in this.finalGoalsContainer.GetChildren())
        {
            this.finalGoalsContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (Node child in this.wallsContainer.GetChildren())
        {
            this.wallsContainer.RemoveChild(child);
            child.QueueFree();
        }

        #endregion

        #region set phase

        this.totalBoardsLabel.Text = $"Boards: {this.solvedBoardsCount}";
        this.totalMovesLabel.Text = $"Moves: {this.totalMovesCount}";

        board = Board.CreateBoardRandom(4);

        Goal? currentGoal = this.board.GetGoal();
        List<Goal> goals = this.board.GetAllGoals();

        if (currentGoal == null)
        {
            throw new Exception("Missing goal");
        }

        Node? currentGoalShape = this.InstantiateGoalScene(currentGoal.Shape);
        this.SetGoal(currentGoalShape as Sprite2D, 8, 7, currentGoal.Robot);
        this.finalGoalsContainer.AddChild(currentGoalShape);

        Sprite2D? currentGoalRobot = this.InstantiateRobotScene(currentGoal.Robot);
        if (currentGoalRobot != null)
        {
            currentGoalRobot.Position = this.tileMapLayer.MapToLocal(new Vector2I(7, 7));
            this.finalGoalsContainer.AddChild(currentGoalRobot);
        }

        foreach (Goal goal in goals)
        {
            Node? goalScene = this.InstantiateGoalScene(goal.Shape);

            if (goalScene != null)
            {
                this.SetGoal(goalScene as Sprite2D, goal.X, goal.Y, goal.Robot);
                this.goalsContainer.AddChild(goalScene);

                if (goal == currentGoal)
                {
                    if (goalScene is Sprite2D sprite)
                    {
                        ShaderMaterial sm = new()
                        {
                            Shader = ResourceLoader.Load<Shader>("res://shaders/goal_background.gdshader")
                        };
                        sm.SetShaderParameter("background_color", White);
                        sprite.Material = sm;
                    }
                }
            }
        }

        this.SetRobotPosition(ROBOT.RED);
        this.SetRobotPosition(ROBOT.GREEN);
        this.SetRobotPosition(ROBOT.BLUE);
        this.SetRobotPosition(ROBOT.YELLOW);

        this.SetSelectedRobot(currentGoal?.Robot);

        var walls = board.GetWalls();

        for (int dir = 0; dir < walls.GetLength(0); ++dir)
        {
            for (int pos = 0; pos < walls.GetLength(1); ++pos)
            {
                if (walls[dir, pos])
                {
                    this.SetWall((DIRECTION)dir, pos);
                }
            }
        }

        #endregion
    }

    private void SetSelectedRobot(ROBOT? robot)
    {
        // vortex robot
        if (robot.HasValue && !robot.Value.IsSingleRobot())
        {
            return;
        }

        if (this.selectedRobot.HasValue)
        {
            this.SetRobotShaderParam(this.selectedRobot.Value, Transparent);
        }

        this.selectedRobot = robot;

        if (this.selectedRobot.HasValue)
        {
            this.SetRobotShaderParam(this.selectedRobot.Value, White);
        }
    }

    private void SetRobotShaderParam(ROBOT robot, Color color)
    {
        Sprite2D sprite = this.robotSprites[robot];
        if (sprite.Material is ShaderMaterial sm)
        {
            sm.SetShaderParameter("background_color", color);
        }
    }

    private void MoveRobot(DIRECTION direction)
    {
        if (this.isFrozen || !this.selectedRobot.HasValue)
        {
            return;
        }

        Board.Move? move = this.board.MoveRobot(this.selectedRobot.Value, direction);
        if (move != null)
        {
            this.SetRobotPosition(this.selectedRobot.Value);

            this.totalMovesCount++;
            this.totalMovesLabel.Text = $"Moves: {this.totalMovesCount}";
            this.currentBoardMoves.Push(move);

            if (this.board.IsSolved())
            {
                EmitSignal(SignalName.BoardSolved);
            }
        }
    }

    private void UndoMoveRobot()
    {
        if (this.currentBoardMoves.Count > 0)
        {

            Board.Move move = this.currentBoardMoves.Pop();
            this.SetSelectedRobot(move.Robot);

            this.board.MoveRobotToPosition(this.selectedRobot!.Value, move.OldPosition);
            this.SetRobotPosition(this.selectedRobot.Value);

            this.totalMovesCount--;
            this.totalMovesLabel.Text = $"Moves: {this.totalMovesCount}";
        }
    }

    private void ResetBoard()
    {
        while (this.currentBoardMoves.Count > 0)
        {
            this.UndoMoveRobot();

            Goal? currentGoal = this.board.GetGoal();
            this.SetSelectedRobot(currentGoal?.Robot ?? null);
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
        this.timesUpLabel.Show();
        if (this.solvedBoardsCount > 0)
        {
            this.globalRR.CallDeferred("save_score", this.solvedBoardsCount);
        }
    }

    private Node? InstantiateGoalScene(SHAPE shape)
    {
        Node? scene = null;

        switch (shape)
        {
            case SHAPE.VORTEX:
                scene = this.vortextGoalScene.Instantiate();
                break;
            case SHAPE.CIRCLE:
                scene = this.circleGoalScene.Instantiate();
                break;
            case SHAPE.TRIANGLE:
                scene = this.triangleGoalScene.Instantiate();
                break;
            case SHAPE.SQUARE:
                scene = this.squareGoalScene.Instantiate();
                break;
            case SHAPE.HEXAGON:
                scene = this.hexagonGoalScene.Instantiate();
                break;
        }

        return scene;
    }

    private Sprite2D? InstantiateRobotScene(ROBOT robot)
    {
        if (!robot.IsSingleRobot())
        {
            return null;
        }

        Sprite2D? scene = null;

        switch (robot)
        {
            case ROBOT.RED:
                scene = this.redRobotScene.Instantiate() as Sprite2D;
                break;
            case ROBOT.GREEN:
                scene = this.greenRobotScene.Instantiate() as Sprite2D;
                break;
            case ROBOT.BLUE:
                scene = this.blueRobotScene.Instantiate() as Sprite2D;
                break;
            case ROBOT.YELLOW:
                scene = this.yellowRobotScene.Instantiate() as Sprite2D;
                break;
            default:
                break;
        }

        return scene;
    }

    private void SetWall(DIRECTION direction, int position)
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
            line.Width = 6f;
            line.DefaultColor = new Godot.Color(0, 0, 0);
            wallsContainer.AddChild(line);
        }
    }

    private void SetRobotPosition(ROBOT robot)
    {
        Sprite2D robotSprite = this.robotSprites[robot];
        int robotPosition = this.board.GetRobotPosition(robot);
        (int robotX, int robotY) = this.board.GetPositionAsXY(robotPosition);
        robotSprite.Position = this.tileMapLayer.MapToLocal(new Vector2I(robotX, robotY));
    }

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
