using System.Diagnostics;

string file = args.Length == 1 ? args[0] : "eg.txt";

var cubes = File.ReadLines(file)
    .Select(line => line.Split(','))
    .Select(numstrs => numstrs.Select(str => int.Parse(str)))
    .Select(numlist => numlist.ToArray())
    .Select(numarr => new Point(numarr[0], numarr[1], numarr[2]))
    .ToHashSet();

Console.WriteLine($"droplet contains {cubes.Count()} cubes");

var bounding = BoundingBox.FromPoints(cubes);

Console.WriteLine($"droplet is bound within {bounding}");
Console.WriteLine($"there are {bounding.Area()} total points inside the bounding box");

// flood fill all the points, to colour in (mark as cubes) any points that are
// entirely within the droplet. this is initially slow - we're visiting a lot of
// points! - but should get faster as we colour in more cubes in the hashset.
var sw = Stopwatch.StartNew();
var points = bounding.Points().ToArray();
for (int i = 0; i < points.Length; i++)
{
    if (cubes.Contains(points[i]))
    {
        // we're adding to cubes as we go, so need to check every iteration.
        continue;
    }

#if DEBUG
    Console.WriteLine($"{sw.Elapsed}: filling {points[i]} ({i}/{points.Length})");
#endif

    FloodFill(points[i]);
}

Console.WriteLine($"droplet contains {cubes.Count()} cubes");

int area = cubes.Select(SurfaceArea).Sum();
Console.WriteLine($"total surface area: {area}");

int SurfaceArea(Point cube) => cube.OrthogonalNeighbours().Count(nb => !cubes.Contains(nb));

void FloodFill(Point start)
{
    if (cubes.Contains(start))
    {
        throw new Exception("must fill starting from an unoccupied point");
    }

    var frontier = new Stack<Point>();
    var visited = new HashSet<Point>();

    // if the point we're filling connects to the outside of the droplet (i.e.
    // if we see a point outside of the bounding box) then there's a path from
    // this point (and from every visited point) to the outside of the droplet.
    bool external = false;

    frontier.Push(start);
    while (frontier.Any())
    {
        var point = frontier.Pop();

        if (point.X < bounding.MinX || point.X > bounding.MaxX ||
            point.Y < bounding.MinY || point.Y > bounding.MaxY ||
            point.Z < bounding.MinZ || point.Z > bounding.MaxZ)
        {
            external = true;
            continue; // stop filling, or we'll recurse forever
        }

        var visit = point
            .OrthogonalNeighbours()
            .Where(p => !visited.Contains(p))
            .Where(p => !cubes.Contains(p));

        foreach (var nb in visit)
        {
            frontier.Push(nb);
        }

        visited.Add(point);
    }

    // if the visited points do not connect to the outside of the droplet, then
    // all the visited points must be internal to the droplet (i.e. we have an
    // air pocket).
    if (!external)
    {
        // add all visited points to our cubes.
        foreach (var point in visited)
        {
            cubes.Add(point);
        }
    }
}

record Point(int X, int Y, int Z)
{
    public IEnumerable<Point> OrthogonalNeighbours() => new Point[6] {
        new (X + 1, Y, Z),
        new (X - 1, Y, Z),
        new (X, Y + 1, Z),
        new (X, Y - 1, Z),
        new (X, Y, Z + 1),
        new (X, Y, Z - 1),
    };
}

record BoundingBox(int MinX, int MaxX, int MinY, int MaxY, int MinZ, int MaxZ)
{
    public static BoundingBox FromPoints(IEnumerable<Point> points) =>
        points.Aggregate(
            new BoundingBox(MinX: int.MaxValue, MaxX: int.MinValue,
                            MinY: int.MaxValue, MaxY: int.MinValue,
                            MinZ: int.MaxValue, MaxZ: int.MinValue),
            (acc, cube) => acc with
            {
                MinX = Math.Min(acc.MinX, cube.X),
                MaxX = Math.Max(acc.MaxX, cube.X),
                MinY = Math.Min(acc.MinY, cube.Y),
                MaxY = Math.Max(acc.MaxY, cube.Y),
                MinZ = Math.Min(acc.MinZ, cube.Z),
                MaxZ = Math.Max(acc.MaxZ, cube.Z)
            }
        );

    public int Area() => (MaxX - MinX) * (MaxY - MinY) * (MaxZ - MinZ);

    public IEnumerable<Point> Points()
    {
        for (int x = MinX; x < MaxX; x++)
        {
            for (int y = MinY; y < MaxY; y++)
            {
                for (int z = MinZ; z < MaxZ; z++)
                {
                    yield return new Point(x, y, z);
                }
            }
        }
    }
}
