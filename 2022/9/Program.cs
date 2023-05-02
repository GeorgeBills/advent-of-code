using System.Text.RegularExpressions;

var motionRegex = new Regex(@"^([UDLR]) (\d+)$");

var motions = File.ReadLines(args[0])
    .Select(line => motionRegex.Match(line).Groups)
    .Select(group =>
        new Motion(
            parseDirection(group[1].Value),
            Steps: int.Parse(group[2].Value)
        )
    );

var rope = new Rope();

foreach (var motion in motions)
{
    rope.Move(motion);
}

Console.WriteLine($"rope is at {rope.HeadPosition()}, tail visited {rope.NumTailVisits()} positions");

Direction parseDirection(string s) => s switch
{
    "R" => Direction.Right,
    "U" => Direction.Up,
    "L" => Direction.Left,
    "D" => Direction.Down,
    _ => throw new ArgumentException($"unhandled motion: {s}"),
};

enum Direction { Up, Down, Left, Right };

readonly record struct Motion(Direction Direction, int Steps);

readonly record struct Vector(int X, int Y)
{
    public Vector Abs() => this with { X = Math.Abs(this.X), Y = Math.Abs(this.Y) };
}

readonly record struct Position(int X, int Y)
{
    public Position Moved(Direction d) => d switch
    {
        Direction.Up => this with { Y = this.Y + 1 },
        Direction.Down => this with { Y = this.Y - 1 },
        Direction.Left => this with { X = this.X - 1 },
        Direction.Right => this with { X = this.X + 1 },
        _ => throw new Exception($"unhandled direction: {d}"),
    };

    public Vector VectorTo(Position other) => new Vector(other.X - this.X, other.Y - this.Y);
}

class Rope
{
    public const int NumKnots = 10; // "consisting of ten knots"

    private Position[] Knots { get; init; }
    private ISet<Position> TailVisits { get; init; }

    public Rope()
    {
        this.Knots = new Position[NumKnots];
        this.TailVisits = new HashSet<Position>();
    }

    public int NumTailVisits() => TailVisits.Count();

    public Position HeadPosition() => Knots[0];

    public void Move(Motion motion)
    {
        // Console.WriteLine($"== {motion.Direction} {motion.Steps} ==");

        for (int i = 0; i < motion.Steps; i++)
        {
            // "One knot is still the head of the rope and moves according to
            // the series of motions."
            Knots[0] = Knots[0].Moved(motion.Direction);

            // "Each knot further down the rope follows the knot in front of it
            // using the same rules as before."
            for (int j = 1; j < Knots.Length; j++)
            {
                var vector = Knots[j].VectorTo(Knots[j - 1]);
                Knots[j] = vector switch
                {
                    // "If the head is ever two steps directly up, down, left, or
                    // right from the tail, the tail must also move one step in that
                    // direction so it remains close enough."
                    { X: +2, Y: 0 } => Knots[j].Moved(Direction.Right),
                    { X: 0, Y: +2 } => Knots[j].Moved(Direction.Up),
                    { X: -2, Y: 0 } => Knots[j].Moved(Direction.Left),
                    { X: 0, Y: -2 } => Knots[j].Moved(Direction.Down),

                    // "Otherwise, if the head and tail aren't touching and aren't
                    // in the same row or column, the tail always moves one step
                    // diagonally to keep up."
                    { X: +2, Y: +1 } => Knots[j].Moved(Direction.Right).Moved(Direction.Up),
                    { X: +2, Y: -1 } => Knots[j].Moved(Direction.Right).Moved(Direction.Down),
                    { X: +1, Y: +2 } => Knots[j].Moved(Direction.Right).Moved(Direction.Up),
                    { X: +1, Y: -2 } => Knots[j].Moved(Direction.Right).Moved(Direction.Down),
                    { X: -2, Y: +1 } => Knots[j].Moved(Direction.Left).Moved(Direction.Up),
                    { X: -2, Y: -1 } => Knots[j].Moved(Direction.Left).Moved(Direction.Down),
                    { X: -1, Y: +2 } => Knots[j].Moved(Direction.Left).Moved(Direction.Up),
                    { X: -1, Y: -2 } => Knots[j].Moved(Direction.Left).Moved(Direction.Down),

                    { X: +2, Y: +2 } => Knots[j].Moved(Direction.Right).Moved(Direction.Up),
                    { X: +2, Y: -2 } => Knots[j].Moved(Direction.Right).Moved(Direction.Down),
                    { X: -2, Y: +2 } => Knots[j].Moved(Direction.Left).Moved(Direction.Up),
                    { X: -2, Y: -2 } => Knots[j].Moved(Direction.Left).Moved(Direction.Down),

                    // touching
                    { X: >= -1 and <= 1, Y: >= -1 and <= 1 } => Knots[j],

                    _ => throw new Exception($"unhandled vector {vector} between knot {KnotToChar(j)} @ {Knots[j]} and knot {KnotToChar(j - 1)} @ {Knots[j - 1]}"),
                };
            }

            // Plot(new Position(6, 6));
            // Console.WriteLine();

            TailVisits.Add(Knots[^1]);
        }
    }

    private char KnotToChar(int n) => n switch
    {
        0 => 'H',
        >= 1 and <= 9 => (char)('0' + n),
        _ => throw new Exception($"unsupported knot n: {n}"),
    };

    public void Plot(Position max)
    {
        const char emptyChar = '.';

        var grid = new char[max.X, max.Y];

        // populate background
        for (int i = 0; i < max.X; i++)
        {
            for (int j = 0; j < max.Y; j++)
            {
                grid[i, j] = emptyChar;
            }
        }

        // populate rope
        for (int i = 0; i < Knots.Length; i++)
        {
            var knot = Knots[i];

            if (grid[knot.Y, knot.X] == emptyChar)
            {
                grid[knot.Y, knot.X] = KnotToChar(i);
            }
        }

        // print grid
        for (int i = max.X - 1; i >= 0; i--)
        {
            for (int j = 0; j < max.Y; j++)
            {
                Console.Write(grid[i, j]);
            }
            Console.WriteLine();
        }
    }
}
