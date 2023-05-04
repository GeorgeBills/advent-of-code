var lines = File.ReadAllLines(args.Length >= 1 ? args[0] : "eg.txt");
(char[,] grid, Dimensions dimensions, Point start, Point end) = ParseGrid(lines);
PrintGrid(grid, dimensions);
(var path, int distance) = Search(dimensions, start, end);
Console.WriteLine($"shortest path distance is {distance}");

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

(IEnumerable<Point> path, int distance) Search(Dimensions dimensions, Point start, Point end)
{
    (int height, int width) = dimensions;

    var frontier = new PriorityQueue<Node, int>();
    var startNode = new Node(start, Previous: null, TotalDistance: 0);
    frontier.Enqueue(startNode, start.ManhattanDistance(end));

    var seen = new HashSet<Point>();

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
            if (seen.Contains(neighbour))
            {
                continue; // already seen this node
            }

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
            var neighbourNode = new Node(neighbour, Previous: currentNode, distanceToNeighbour);
            int estimatedTotalDistance = distanceToNeighbour + neighbour.ManhattanDistance(end);
            frontier.Enqueue(neighbourNode, estimatedTotalDistance);

            seen.Add(neighbour);
        }
    }

    throw new Exception($"no path to {end} from {start}!");

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
