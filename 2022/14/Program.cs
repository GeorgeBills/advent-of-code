string file = args.Length == 1 ? args[0] : "eg.txt";

var paths = File.ReadAllLines(file).Select(line => ParsePath(line));

var occupied = GetOccupied(paths);

var sand = new Point(500, 0); // "The sand is pouring into the cave from point 500,0."

#if DEBUG
var dimensions = occupied
    .Append(sand) // make sure we include the sand point in the diagram
    .Aggregate(
        new { MinX = int.MaxValue, MinY = int.MaxValue, MaxX = int.MinValue, MaxY = int.MinValue },
        (acc, point) => new
        {
            MinX = Math.Min(acc.MinX, point.X),
            MinY = Math.Min(acc.MinY, point.Y),
            MaxX = Math.Max(acc.MaxX, point.X),
            MaxY = Math.Max(acc.MaxY, point.Y),
        }
    );

var min = new Point(X: dimensions.MinX, Y: dimensions.MinY);
var max = new Point(X: dimensions.MaxX, Y: dimensions.MaxY);

PrintDiagram(min, max, occupied, sand);
#endif

IEnumerable<Point> ParsePath(string pathstr)
    => pathstr.Split(" -> ").Select(pointstr => ParsePoint(pointstr));

Point ParsePoint(string pointstr)
{
    var coords = pointstr.Split(',').Select(coordstr => int.Parse(coordstr));
    if (coords.Count() != 2)
    {
        throw new Exception("expected exactly two coordinates");
    }
    return new Point(X: coords.First(), Y: coords.Last());
}

HashSet<Point> GetOccupied(IEnumerable<IEnumerable<Point>> paths)
{
    var occupied = new HashSet<Point>();

    foreach (var path in paths)
    {
        var pathx = path;
        while (pathx.Count() >= 2)
        {
            var pair = pathx.Take(2);

            var (from, to) = (pair.First(), pair.Last());

            var direction = GetDirection(from, to);

            occupied.Add(from);
            occupied.Add(to);
            var current = from;
            while (current != to)
            {
                current = Next(current, direction);
                occupied.Add(current);
            }

            pathx = pathx.Skip(1); // drop the leading coordinate
        }
    }

    return occupied;
}

Direction GetDirection(Point fromPoint, Point toPoint) => (fromPoint, toPoint) switch
{
    var (a, b) when a.X == b.X && a.Y < b.Y => Direction.Up,
    var (a, b) when a.X == b.X && a.Y > b.Y => Direction.Down,
    var (a, b) when a.X > b.X && a.Y == b.Y => Direction.Left,
    var (a, b) when a.X < b.X && a.Y == b.Y => Direction.Right,
    _ => throw new Exception($"neither horizontal nor vertical: {fromPoint} {toPoint}"), // either the same or diagonal
};

Point Next(Point fromPoint, Direction direction) => direction switch
{
    Direction.Up => fromPoint with { Y = fromPoint.Y + 1 },
    Direction.Down => fromPoint with { Y = fromPoint.Y - 1 },
    Direction.Left => fromPoint with { X = fromPoint.X - 1 },
    Direction.Right => fromPoint with { X = fromPoint.X + 1 },
    _ => throw new Exception($"{direction} unhandled"),
};

void PrintDiagram(Point min, Point max, ISet<Point> occupied, Point sand)
{
    int width = max.X - min.X + 1;
    int height = max.Y - min.Y + 1;

    char[,] grid = new char[height, width];

    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            int x = j + min.X;
            int y = i + min.Y;
            var point = new Point(x, y);
            char draw;
            if (occupied.Contains(point))
            {
                draw = '#';
            }
            else if (sand == point)
            {
                draw = '+';
            }
            else
            {
                draw = '.';
            }
            Console.Write(draw);
        }
        Console.WriteLine();
    }
}

readonly record struct Point(int X, int Y)
{
    public override string ToString() => $"{X},{Y}";
}

enum Direction { Up, Down, Left, Right }
