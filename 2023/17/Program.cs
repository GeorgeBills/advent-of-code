const string file = "in.txt";

var map = ParseMap(file);

PrintMap(map);

const int minHeat = 1;
const int maxConsecutiveBeforeTurning = 10;
const int minConsecutiveBeforeTurning = 4;

var (height, width) = (map.GetLength(0), map.GetLength(1));

var startpos = new Position(0, 0);                // "the top-left city block"
var endpos = new Position(height - 1, width - 1); // "the bottom-right city block"

var starteast = new Node(startpos, Direction.E);
var startsouth = new Node(startpos, Direction.S);

var costs = new Dictionary<Node, int> {
    { starteast, 0 },
    { startsouth, 0},
};

var previouses = new Dictionary<Node, Node>();

var frontier = new PriorityQueue<Node, int>();
int startcost = EstimateCost(startpos, endpos);
frontier.Enqueue(starteast, startcost);
frontier.Enqueue(startsouth, startcost);

while (frontier.TryDequeue(out Node current, out int _))
{
#if DEBUG
    Console.WriteLine($"{current} {frontier.Count}");
#endif

    if (current.Position == endpos &&
        current.Consecutive > minConsecutiveBeforeTurning /* "or even before it can stop at the end" !!! */ )
    {
        int cost = costs[current];

        var path = new List<Node> { current };
        while (previouses.TryGetValue(current, out Node prev))
        {
            current = prev;
            path.Add(prev);
        }
#if DEBUG
        PrintPath(path, map);
#endif

        Console.WriteLine($"found end position with cost {cost} and total path length {path.Count}");

        break;
    }

    var expanded = Expand(current, height - 1, width - 1);
    foreach (var node in expanded)
    {
        int costg = costs[current] + map[node.Position.Row, node.Position.Column];

        if (!costs.TryGetValue(node, out int costgprev) /* haven't evaluated this neighbour previously */ ||
            costgprev > costg                           /* previous evaluation was more expensive */)
        {
            // record best known path to neighbour
            previouses[node] = current;
            costs[node] = costg;

            // enqueue neighbour to expand
            int costh = EstimateCost(node.Position, endpos);
            frontier.Enqueue(node, costg + costh /* actual plus estimate */);
        }
    }
}

static void PrintPath(IEnumerable<Node> path, int[,] map)
{
    var dirs = path.ToDictionary(n => n.Position, n => n.Direction);
    var (height, width) = (map.GetLength(0), map.GetLength(1));
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            var pos = new Position(row, col);

            char c = dirs.TryGetValue(pos, out Direction dir)
                ? dir switch { Direction.N => '^', Direction.S => 'v', Direction.E => '>', Direction.W => '<' }
                : (char)(map[row, col] + '0');

            Console.Write(c);
        }
        Console.WriteLine();
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

    // "a maximum of ten consecutive blocks without turning"
    if (node.Consecutive >= maxConsecutiveBeforeTurning)
    {
        neighbours = neighbours.Where(n => n.Direction != node.Direction);
    }

    // "needs to move a minimum of four blocks in that direction before it can turn"
    if (node.Consecutive < minConsecutiveBeforeTurning)
    {
        neighbours = neighbours.Where(n => n.Direction == node.Direction);
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

static int ManhattanDistance(Position from, Position to) => Math.Abs(to.Row - from.Row) + Math.Abs(to.Column - from.Column);

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
