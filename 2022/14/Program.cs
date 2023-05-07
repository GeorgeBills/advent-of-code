string file = args.Length == 1 ? args[0] : "eg.txt";

var paths = File.ReadAllLines(file).Select(line => ParsePath(line));

var occupiedRock = GetOccupied(paths);

// "The sand is pouring into the cave from point 500,0."
var sandSource = new Point(500, 0);

var (min, max) = GetDimensions(occupiedRock, sandSource);

var occupiedSand = new HashSet<Point>();

#if DEBUG
Console.WriteLine("before simulation:");
PrintDiagram(min, max, occupiedRock, occupiedSand, sandSource);
#endif

int tick;
Point? sand = null;
for (tick = 0; ; tick++)
{
    if (sand == null)
    {
        sand = sandSource;
    }

    // "Sand keeps moving as long as it is able to do so, at each step trying to
    // move down, then down-left, then down-right."

    var down = Next((Point)sand, Direction.Down);
    var downLeft = Next((Point)sand, Direction.DownLeft);
    var downRight = Next((Point)sand, Direction.DownRight);

    // "A unit of sand always falls down one step if possible."
    if (!IsOccupied(down, occupiedRock, occupiedSand))
    {
        sand = down;
    }

    // "If the tile immediately below is blocked (by rock or sand), the unit of
    // sand attempts to instead move diagonally one step down and to the left."
    else if (!IsOccupied(downLeft, occupiedRock, occupiedSand))
    {
        sand = downLeft;
    }

    // "If that tile is blocked, the unit of sand attempts to instead move
    // diagonally one step down and to the right."
    else if (!IsOccupied(downRight, occupiedRock, occupiedSand))
    {
        sand = downRight;
    }

    // "If all three possible destinations are blocked, the unit of sand comes
    // to rest and no longer moves, at which point the next unit of sand is
    // created back at the source."
    else
    {
        occupiedSand.Add((Point)sand);
        sand = null;
    }

    // "...sand flows out the bottom, falling into the endless void"
    //
    // Once we've started overflowing, no more sand will ever come to rest so we
    // end the simulation.
    if (sand?.Y > max.Y)
    {
        break;
    }
}

#if DEBUG
Console.WriteLine($"after simulation ({tick + 1} ticks processed)");
PrintDiagram(min, max, occupiedRock, occupiedSand, sandSource);
#endif

Console.WriteLine($"{occupiedSand.Count()} units of sand have come to rest");

IEnumerable<Point> ParsePath(string pathstr)
    => pathstr.Split("-> ").Select(pointstr => ParsePoint(pointstr));

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

    var pathsarr = paths.ToArray();

    for (int i = 0; i < pathsarr.Length; i++)
    {
        var path = pathsarr[i];

        while (path.Count() >= 2)
        {
            var pair = path.Take(2);

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

            path = path.Skip(1); // drop the leading coordinate
        }
    }

    return occupied;
}

Direction GetDirection(Point fromPoint, Point toPoint) => (fromPoint, toPoint) switch
{
    // "y represents distance down" (not up!)
    var (a, b) when a.X == b.X && a.Y > b.Y => Direction.Up,
    var (a, b) when a.X == b.X && a.Y < b.Y => Direction.Down,
    var (a, b) when a.X > b.X && a.Y == b.Y => Direction.Left,
    var (a, b) when a.X < b.X && a.Y == b.Y => Direction.Right,
    _ => throw new Exception($"neither horizontal nor vertical: {fromPoint} {toPoint}"), // either the same or diagonal
};

Point Next(Point fromPoint, Direction direction) => direction switch
{
    // "y represents distance down" (not up!)
    Direction.Up => fromPoint with { Y = fromPoint.Y - 1 },
    Direction.Down => fromPoint with { Y = fromPoint.Y + 1 },
    Direction.Left => fromPoint with { X = fromPoint.X - 1 },
    Direction.Right => fromPoint with { X = fromPoint.X + 1 },
    Direction.DownLeft => fromPoint with { X = fromPoint.X - 1, Y = fromPoint.Y + 1 },
    Direction.DownRight => fromPoint with { X = fromPoint.X + 1, Y = fromPoint.Y + 1 },
    _ => throw new Exception($"{direction} unhandled"),
};

(Point min, Point max) GetDimensions(HashSet<Point> occupied, Point sand)
{
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

    min = new Point(X: dimensions.MinX, Y: dimensions.MinY);
    max = new Point(X: dimensions.MaxX, Y: dimensions.MaxY);

    return (min, max);
}

void PrintDiagram(Point min, Point max, ISet<Point> occupiedRock, ISet<Point> occupiedSand, Point sandSource)
{
    int width = max.X - min.X + 1;
    int height = max.Y - min.Y + 1;
    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            int x = j + min.X;
            int y = i + min.Y;
            var point = new Point(x, y);
            char draw;
            if (occupiedRock.Contains(point))
            {
                draw = '#';
            }
            else if (occupiedSand.Contains(point))
            {
                draw = 'o';
            }
            else if (sandSource == point)
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

bool IsOccupied(Point p, params ISet<Point>[] occupied) => occupied.Any(o => o.Contains(p));

readonly record struct Point(int X, int Y)
{
    public override string ToString() => $"{X},{Y}";
}

enum Direction { Up, Down, Left, Right, DownLeft, DownRight }
