string file = args.Length == 1 ? args[0] : "egsimple.txt";

var valley = ParseInput(file);

PrintValleyTiles(valley);

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

valley = null; // dead to us

var current = start;

for (int i = 0; i < 6; i++)
{
    PrintValley(blizzards, start, finish, current, dimensions, i);
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
                        int round)
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
                var posblizzards = ContainsBlizzard(blizzards, dimensions, pos, round).ToArray();
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

    static int modulus(int dividend, int divisor) => ((dividend % divisor) + divisor) % divisor;

    static IEnumerable<Tile> ContainsBlizzard(Tile[,] blizzards,
                                              Dimensions dimensions,
                                              Position pos,
                                              int round)
    {
        // number of rows and columns minus the walls
        var overlaydimensions = dimensions with
        {
            Rows = dimensions.Rows - 2,
            Columns = dimensions.Columns - 2
        };

        // subtract one to account for the missing wall
        var overlaypos = pos with
        {
            Row = pos.Row - 1,
            Column = pos.Column - 1,
        };

        int upblizzrow = modulus(overlaypos.Row + round, overlaydimensions.Rows);
        if (blizzards[upblizzrow, overlaypos.Row] == Tile.BlizzardUp)
        {
            yield return Tile.BlizzardUp;
        }

        int downblizzrow = modulus(overlaypos.Row - round, overlaydimensions.Rows);
        if (blizzards[downblizzrow, overlaypos.Column] == Tile.BlizzardDown)
        {
            yield return Tile.BlizzardDown;
        }

        int leftblizzcol = modulus(overlaypos.Column + round, overlaydimensions.Columns);
        if (blizzards[overlaypos.Row, leftblizzcol] == Tile.BlizzardLeft)
        {
            yield return Tile.BlizzardLeft;
        }

        int rightblizzcol = modulus(overlaypos.Column - round, overlaydimensions.Columns);
        if (blizzards[overlaypos.Row, rightblizzcol] == Tile.BlizzardRight)
        {
            yield return Tile.BlizzardRight;
        }
    }
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

record Position(int Row, int Column);

record Dimensions(int Rows, int Columns);
