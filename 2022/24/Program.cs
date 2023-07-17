using System.Diagnostics;

string file = args.Length == 1 ? args[0] : "egsimple.txt";

var valley = ParseInput(file);

#if DEBUG
PrintValleyTiles(valley);
#endif

var dimensions = new Dimensions(valley.GetLength(0), valley.GetLength(1));

Console.WriteLine($"dimensions: {dimensions}");

var start = FindStartPosition(valley);

var finish = FindFinishPosition(valley);

Console.WriteLine($"start: {start}; finish: {finish}");

if (GetColumnPositions(valley, start.Column).Any(pos => PositionContainsVerticallyMovingBlizzard(valley, pos)) ||
    GetColumnPositions(valley, finish.Column).Any(pos => PositionContainsVerticallyMovingBlizzard(valley, pos)))
{
    throw new Exception(); // overlay won't work
                           // wrapping will be funky in the columns containing the start or finish
}

var blizzards = ExtractBlizzardOverlay(valley);


var sw = Stopwatch.StartNew();
(var path, int minutes) = Search(valley, blizzards, dimensions, start, finish);
sw.Stop();

Console.WriteLine($"took {minutes} minutes to find the finish (took {sw.Elapsed})");
Console.WriteLine($"path: {String.Join(" => ", path)}");

foreach (var idxpos in path.Select((pos, i) => (i, pos)))
{
    Console.WriteLine();
    Console.WriteLine($"minute {idxpos.i}");
    PrintValley(blizzards, start, finish, current: idxpos.pos, dimensions, minute: idxpos.i);
}

static Tile[,] ParseInput(string file)
{
    var input = File.ReadLines(file).Select(line => line.ToCharArray()).ToArray();

    int height = input.Length;
    int width = input.Select(line => line.Length).Distinct().Single();

    var valley = new Tile[height, width];

    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            valley[row, col] = (Tile)input[row][col];
        }
    }

    return valley;
}

static void PrintValleyTiles(Tile[,] valley)
{
    for (int row = 0; row < valley.GetLength(0); row++)
    {
        for (int col = 0; col < valley.GetLength(1); col++)
        {
            char draw = (char)valley[row, col];
            Console.Write(draw);
        }
        Console.WriteLine();
    }
}

static IEnumerable<int> RowIndexes(Tile[,] valley) => Enumerable.Range(0, valley.GetLength(0));

static IEnumerable<int> ColumnIndexes(Tile[,] valley) => Enumerable.Range(0, valley.GetLength(1));

static IEnumerable<Position> GetRowPositions(Tile[,] valley, int row) => ColumnIndexes(valley).Select(col => new Position(row, col));

static IEnumerable<Position> GetColumnPositions(Tile[,] valley, int col) => RowIndexes(valley).Select(row => new Position(row, col));

// "Your expedition begins in the only non-wall position in the top row..."
static Position FindStartPosition(Tile[,] valley) =>
    GetRowPositions(valley, row: 0).Single(pos => valley[pos.Row, pos.Column] == Tile.Clear);

// "...and needs to reach the only non-wall position in the bottom row."
static Position FindFinishPosition(Tile[,] valley) =>
    GetRowPositions(valley, row: valley.GetLength(0) - 1).Single(pos => valley[pos.Row, pos.Column] == Tile.Clear);

static bool PositionContainsVerticallyMovingBlizzard(Tile[,] valley, Position pos) =>
    valley[pos.Row, pos.Column] is Tile.BlizzardUp or Tile.BlizzardDown;

static Tile[,] ExtractBlizzardOverlay(Tile[,] valley)
{
    // the overlay ignores the walls bordering the valley
    // this makes the modulo wrapping simpler
    var interior = (
        rows: valley.GetLength(0) - 2,
        cols: valley.GetLength(1) - 2
    );
    var overlay = new Tile[interior.rows, interior.cols];

    // skip the walls in the first and last row and column
    for (int row = 1; row < valley.GetLength(0) - 1; row++)
    {
        for (int col = 1; col < valley.GetLength(1) - 1; col++)
        {
            var tile = valley[row, col];
            overlay[row - 1, col - 1] = tile is Tile.BlizzardUp
                                             or Tile.BlizzardDown
                                             or Tile.BlizzardLeft
                                             or Tile.BlizzardRight
                ? tile
                : Tile.Clear;
        }
    }

    return overlay;
}

static void PrintValley(Tile[,] blizzards,
                        Position start,
                        Position finish,
                        Position current,
                        Dimensions dimensions,
                        int minute)
{
    (int rows, int cols) = dimensions;

    for (int row = 0; row < rows; row++)
    {
        for (int col = 0; col < cols; col++)
        {
            var pos = new Position(row, col);

            char draw;
            if (pos == current)
            {
                draw = 'E';
            }
            else if (pos == start)
            {
                draw = 'S';
            }
            else if (pos == finish)
            {
                draw = 'F';
            }
            else if (row == 0 || row == rows - 1 || col == 0 || col == cols - 1)
            {
                draw = (char)Tile.Wall;
            }
            else
            {
                var posblizzards = ContainsBlizzard(blizzards, dimensions, pos, minute).ToArray();
                draw = posblizzards.Count() switch
                {
                    0 => (char)Tile.Clear,
                    1 => (char)posblizzards.Single(),
                    _ => (char)(posblizzards.Count() + '0'),
                };
            }
            Console.Write(draw);
        }
        Console.WriteLine();
    }
}

static IEnumerable<Tile> ContainsBlizzard(Tile[,] blizzards,
                                          Dimensions dimensions,
                                          Position pos,
                                          int minute)
{
    // subtract one to account for the missing wall
    var overlaypos = pos with
    {
        Row = pos.Row - 1,
        Column = pos.Column - 1,
    };

    // number of rows and columns minus the walls
    var overlaydimensions = dimensions with
    {
        Rows = dimensions.Rows - 2,
        Columns = dimensions.Columns - 2
    };

    if (overlaypos.Row < 0 || overlaypos.Row >= overlaydimensions.Rows ||
        overlaypos.Column < 0 || overlaypos.Column >= overlaydimensions.Columns)
    {
        // out of bounds
        // allowing calls with oob indexes makes calls simpler for the search
        yield break;
    }

    int upblizzrow = modulus(overlaypos.Row + minute, overlaydimensions.Rows);
    if (blizzards[upblizzrow, overlaypos.Column] == Tile.BlizzardUp)
    {
        yield return Tile.BlizzardUp;
    }

    int downblizzrow = modulus(overlaypos.Row - minute, overlaydimensions.Rows);
    if (blizzards[downblizzrow, overlaypos.Column] == Tile.BlizzardDown)
    {
        yield return Tile.BlizzardDown;
    }

    int leftblizzcol = modulus(overlaypos.Column + minute, overlaydimensions.Columns);
    if (blizzards[overlaypos.Row, leftblizzcol] == Tile.BlizzardLeft)
    {
        yield return Tile.BlizzardLeft;
    }

    int rightblizzcol = modulus(overlaypos.Column - minute, overlaydimensions.Columns);
    if (blizzards[overlaypos.Row, rightblizzcol] == Tile.BlizzardRight)
    {
        yield return Tile.BlizzardRight;
    }

    static int modulus(int dividend, int divisor) => ((dividend % divisor) + divisor) % divisor;
}

static (IEnumerable<Position> path, int minutes) Search(Tile[,] valley, Tile[,] blizzards, Dimensions dimensions, Position start, Position finish)
{
    var frontier = new PriorityQueue<SearchNode, int>();
    var initial = new SearchNode(
        position: start,
        minutes: 0,
        predecessor: null
    );
    int estimate = EstimateCost(0, start, finish);
    frontier.Enqueue(initial, estimate);

    int i = 0;
    const int update = 1_000_000;
    while (frontier.Count > 0)
    {
        var explore = frontier.Dequeue();
        i++;

        if (i % update == 0)
        {
            Console.WriteLine($"exploring {explore.position} @ {explore.minutes} mins ({frontier.Count} nodes in frontier)");
        }

        if (explore.position == finish)
        {
            var current = explore;
            var path = new List<Position> { };
            while (current != null)
            {
                path.Add(current.position);
                current = current.predecessor;
            }

            path.Reverse(); // start => finish

            return (path, explore.minutes);
        }

        int nextminute = explore.minutes + 1;

        var candidates = explore.position.NeighboursPlusSelf(dimensions)
            // filter out the walls
            .Where(c => valley[c.Row, c.Column] != Tile.Wall)
            // filter out moving in to a square that WILL contain a blizzard
            .Where(c => !ContainsBlizzard(blizzards, dimensions, c, nextminute).Any())
            .Select(c => (
                Element: new SearchNode(position: c, minutes: nextminute, predecessor: explore),
                Priority: EstimateCost(nextminute, c, finish))
            );

        frontier.EnqueueRange(candidates);
    }

    throw new Exception("ran out of paths to explore without finding the finish");

    static int EstimateCost(int current, Position a, Position b) => current + a.ManhattanDistance(b);
}

enum Tile : ushort
{
    Wall = '#',
    Clear = '.',
    BlizzardUp = '^',
    BlizzardDown = 'v',
    BlizzardLeft = '<',
    BlizzardRight = '>',
}

record Position(int Row, int Column)
{
    public int ManhattanDistance(Position other) =>
        Math.Abs(this.Row - other.Row) +
        Math.Abs(this.Column - other.Column);

    public override string ToString() => $"{Row},{Column}";

    public IEnumerable<Position> NeighboursPlusSelf(Dimensions dimensions)
    {
        if (Row > 0)
        {
            yield return this with { Row = Row - 1 }; // up
        }
        if (Row < dimensions.Rows - 1)
        {
            yield return this with { Row = Row + 1 }; // down
        }
        if (Column > 0)
        {
            yield return this with { Column = Column - 1 }; // left
        }
        if (Column < dimensions.Columns - 1)
        {
            yield return this with { Column = Column + 1 }; // right
        }
        yield return this; // self
    }
}

record Dimensions(int Rows, int Columns);

record SearchNode(Position position, int minutes, SearchNode? predecessor);
