const string file = "eg.txt";

var map = ParseMap(file);

PrintMap(map);

const int minHeat = 1;
const int maxConsecutive = 3;

var (height, width) = (map.GetLength(0), map.GetLength(1));

var startpos = new Position(0, 0);                // "the top-left city block"
var endpos = new Position(height - 1, width - 1); // "the bottom-right city block"

var starteast = new Node(startpos, Direction.E);
var startsouth = new Node(startpos, Direction.S);
int startcost = EstimateCost(startpos, endpos);

var seen = new HashSet<Node>();

var frontier = new PriorityQueue<Node, int>();
frontier.Enqueue(starteast, startcost);
frontier.Enqueue(startsouth, startcost);

while (frontier.TryDequeue(out Node current, out int _))
{
    Console.WriteLine($"{current} {frontier.Count}");

    if (!seen.Add(current))
    {
        continue; // already expanded
    }

    if (current.Position == endpos)
    {
        Console.WriteLine($"found end position");
        break;
    }

    var expanded = Expand(current, height - 1, width - 1);
    foreach (var node in expanded)
    {
        int estcost = EstimateCost(node.Position, endpos);
        frontier.Enqueue(node, estcost);
    }
}

static IEnumerable<(Direction Direction, Position Position)> CardinalNeighbours(Position position, int maxRow, int maxColumn)
{
    if (position.Row > 0) { yield return (Direction.N, position with { Row = position.Row - 1 }); }
    if (position.Row < maxRow) { yield return (Direction.S, position with { Row = position.Row + 1 }); }
    if (position.Column < maxColumn) { yield return (Direction.E, position with { Column = position.Column + 1 }); }
    if (position.Column > 0) { yield return (Direction.W, position with { Column = position.Column - 1 }); }
}

static Direction OppositeDirection(Direction direction) => direction switch
{
    Direction.N => Direction.S,
    Direction.S => Direction.N,
    Direction.E => Direction.W,
    Direction.W => Direction.E,
};

static IEnumerable<Node> Expand(Node node, int maxRow, int maxColumn)
{
    var neighbours = CardinalNeighbours(node.Position, maxRow, maxColumn);

    // "can move at most three blocks in a single direction"
    if (node.Consecutive >= maxConsecutive)
    {
        neighbours = neighbours.Where(n => n.Direction != node.Direction);
    }

    // "can't reverse direction"
    var reverse = OppositeDirection(node.Direction);
    neighbours = neighbours.Where(n => n.Direction != reverse);

    return neighbours.Select(
        n => new Node(
            n.Position,
            n.Direction,
            n.Direction == node.Direction ? node.Consecutive + 1 : 1
        )
    );
}

static int EstimateCost(Position from, Position to) => ManhattanDistance(from, to) * minHeat;

static int ManhattanDistance(Position from, Position to) =>
    Math.Abs(to.Row - from.Row) + Math.Abs(to.Column - from.Column);

static int[,] ParseMap(string file)
{
    string[] lines = File.ReadAllLines(file);
    int height = lines.Length;
    int width = lines.Select(l => l.Length).Distinct().Single();
    var map = new int[height, width];
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            map[row, col] = lines[row][col] - '0';
        }
    }
    return map;
}

static void PrintMap(int[,] map)
{
    var (height, width) = (map.GetLength(0), map.GetLength(1));
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            Console.Write(map[row, col]);
        }
        Console.WriteLine();
    }
}

enum Direction { N, S, E, W }

record Position(int Row, int Column);

record Node(Position Position, Direction Direction, int Consecutive = 0);
