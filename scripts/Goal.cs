namespace GodotSharpRR;

public class Goal
{
    public Goal(int x, int y, ROBOT robot, SHAPE shape)
    {
        this.X = x;
        this.Y = y;
        this.Robot = robot;
        this.Shape = shape;
        this.Position = this.X + this.Y * Board.WIDTH; // ?
    }

    public int X { get; set; }
    public int Y { get; set; }

    public int Position { get; set; }

    public ROBOT Robot { get; set; }
    public SHAPE Shape { get; set; }
}
