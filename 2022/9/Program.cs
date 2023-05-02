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

var rope = new Rope(Head: new Position(0, 0), Tail: new Position(0, 0));
var tailVisits = new HashSet<Position>();

foreach (var motion in motions)
{
    rope = rope.Move(motion, tailVisits);
}

Console.WriteLine($"rope is at {rope}, tail visited {tailVisits.Count()} positions");

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

readonly record struct Rope(Position Head, Position Tail)
{
    public Rope Move(Motion motion, ISet<Position> tailVisits)
    {
        var head = this.Head;
        var tail = this.Tail;
        for (int i = 0; i < motion.Steps; i++)
        {
            head = head.Moved(motion.Direction);
            var vector = tail.VectorTo(head);
            tail = vector switch
            {
                // "If the head is ever two steps directly up, down, left, or
                // right from the tail, the tail must also move one step in that
                // direction so it remains close enough."
                { X: +2, Y: 0 } => tail.Moved(Direction.Right),
                { X: 0, Y: +2 } => tail.Moved(Direction.Up),
                { X: -2, Y: 0 } => tail.Moved(Direction.Left),
                { X: 0, Y: -2 } => tail.Moved(Direction.Down),

                // "Otherwise, if the head and tail aren't touching and aren't
                // in the same row or column, the tail always moves one step
                // diagonally to keep up."
                { X: +2, Y: +1 } => tail.Moved(Direction.Right).Moved(Direction.Up),
                { X: +2, Y: -1 } => tail.Moved(Direction.Right).Moved(Direction.Down),
                { X: +1, Y: +2 } => tail.Moved(Direction.Right).Moved(Direction.Up),
                { X: +1, Y: -2 } => tail.Moved(Direction.Right).Moved(Direction.Down),
                { X: -2, Y: +1 } => tail.Moved(Direction.Left).Moved(Direction.Up),
                { X: -2, Y: -1 } => tail.Moved(Direction.Left).Moved(Direction.Down),
                { X: -1, Y: +2 } => tail.Moved(Direction.Left).Moved(Direction.Up),
                { X: -1, Y: -2 } => tail.Moved(Direction.Left).Moved(Direction.Down),

                // touching
                { X: >= -1 and <= 1, Y: >= -1 and <= 1 } => tail,

                _ => throw new Exception($"unhandled vector: {vector}"),
            };
            tailVisits.Add(tail);
        }
        return new Rope(head, tail);
    }
}
