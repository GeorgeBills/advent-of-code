var lines = File.ReadAllLines(args.Length >= 1 ? args[0] : "eg.txt");
(char[,] grid, Dimensions dimensions, Point start, Point end) = ParseGrid(lines);

#if DEBUG
PrintGrid(grid, dimensions);
#endif

(var path, int distance) = Search(dimensions, start, end);
Console.WriteLine($"shortest path distance to {end} from S ({start}) is {distance}");

// get all candidates
const char wantElevation = 'a';
var candidates = GetPointsWithElevation(grid, dimensions)
    .Where(pwe => pwe.elevation == wantElevation)
    .Select(pwe => pwe.point)
    .Append(start);

Console.WriteLine($"searching {candidates.Count()} total squares...");

// brute force search from all the candidates
// breadth-first-search from the end back to any candidate would be much faster...
var winner = candidates
    .Select(point =>
        {
            (var path, int distance) = Search(dimensions, point, end);
            return new { start = point, path, distance };
        }
    )
    .MinBy(c => c.distance);

if (winner != null)
{
    Console.WriteLine($"shortest path distance to {end} from any '{wantElevation}' is from {winner.start} with distance {winner.distance}");
}

(char[,] grid, Dimensions dimensions, Point start, Point end) ParseGrid(string[] lines)
{
    Point? start = null, end = null;
    int height = lines.Length;
    int width = lines.Select(line => line.Length).Distinct().Single();
    var grid = new char[width, height];
    for (int i = 0; i < height; i++)
    {
        string line = lines[i];
        for (int j = 0; j < width; j++)
        {
            int x = j, y = height - 1 - i;
            char c = line[j];
            switch (c)
            {
                case 'S':
                    start = new Point(x, y);
                    break;
                case 'E':
                    end = new Point(x, y);
                    break;
            }
            grid[x, y] = c;
        }
    }
    if (start == null) throw new Exception("no start!");
    if (end == null) throw new Exception("no end!");
    return (grid, new Dimensions(height, width), start, end);
}

void PrintGrid(char[,] grid, Dimensions dimensions)
{
    (int height, int width) = dimensions;

    for (int y = height - 1; y >= 0; y--)
    {
        for (int x = 0; x < width; x++)
        {
            Console.Write(grid[x, y]);
        }
        Console.WriteLine();
    }
}

IEnumerable<(Point point, char elevation)> GetPointsWithElevation(char[,] grid, Dimensions dimensions)
{
    (int height, int width) = dimensions;

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Point point = new Point(x, y);
            char elevation = grid[x, y];
            yield return (point, elevation);
        }
    }
}

(IEnumerable<Point> path, int distance) Search(Dimensions dimensions, Point start, Point end)
{
    (int height, int width) = dimensions;

    var frontier = new PriorityQueue<Node, int>();
    var startNode = new Node(start, Previous: null, TotalDistance: 0);
    frontier.Enqueue(startNode, start.ManhattanDistance(end));

    var seen = new Dictionary<Point, int>();

    while (frontier.Count > 0)
    {
        var currentNode = frontier.Dequeue();

        if (currentNode.Point == end)
        {
            var path = RebuildPathBackToStart(currentNode);
            return (path.Reverse(), currentNode.TotalDistance);
        }

        var neighbours = new Point[]{
            new (currentNode.Point.X, currentNode.Point.Y + 1), // north
            new (currentNode.Point.X, currentNode.Point.Y - 1), // south
            new (currentNode.Point.X - 1, currentNode.Point.Y), // east
            new (currentNode.Point.X + 1, currentNode.Point.Y), // west
        };

        char currentZ = GetHeight(currentNode.Point);

        foreach (var neighbour in neighbours)
        {
            bool valid =
                0 <= neighbour.X && neighbour.X < width &&
                0 <= neighbour.Y && neighbour.Y < height;

            if (!valid)
            {
                continue; // off the grid
            }

            char neighbourZ = GetHeight(neighbour);
            bool visitable = (neighbourZ - currentZ) <= 1;

            if (!visitable)
            {
                continue; // cannot transit from current to neighbour
            }

            int distanceToNeighbour = currentNode.TotalDistance + 1;

            if (seen.TryGetValue(neighbour, out int seenDistance) && seenDistance <= distanceToNeighbour)
            {
                // already seen a shorter or equally short path to this node
                continue;
            }

            seen[neighbour] = distanceToNeighbour;

            var neighbourNode = new Node(neighbour, Previous: currentNode, distanceToNeighbour);
            int estimatedTotalDistance = distanceToNeighbour + neighbour.ManhattanDistance(end);
            frontier.Enqueue(neighbourNode, estimatedTotalDistance);
        }
    }

    return (Enumerable.Empty<Point>(), int.MaxValue); // no path found

    IEnumerable<Point> RebuildPathBackToStart(Node current)
    {
        var path = new List<Point> { current.Point };
        var previous = current.Previous;
        while (previous != null)
        {
            path.Add(previous.Point);
            previous = previous.Previous;
        }
        return path;
    }

    char GetHeight(Point neighbour) => grid[neighbour.X, neighbour.Y] switch
    {
        'S' => 'a', // "Your current position (S) has elevation a...
        'E' => 'z', // "...the location that should get the best signal (E) has elevation z."
        var height => height,
    };
}

record Node(Point Point, Node? Previous, int TotalDistance);

record Point(int X, int Y)
{
    public int ManhattanDistance(Point other) => Math.Abs(this.X - other.X) + Math.Abs(this.Y - other.Y);
    public override string ToString() => $"{X},{Y}";
}

record Dimensions(int height, int width);
