const string file = "in.txt";

var tiles = ParseTiles(file);

PrintTileGrid(tiles);

var start = FindStart(tiles);

var depths = Search(tiles, start);

PrintDepthGrid(depths);

int max = MaxDepth(depths);

Console.WriteLine($"the furthest point from the start is {max} tiles away");

static Tile[,] ParseTiles(string file)
{
    var lines = File.ReadAllLines(file);

    int height = lines.Length;
    int width = lines.Select(l => l.Length).Distinct().Single();

    var tiles = new Tile[height, width];
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            tiles[row, col] = ParseTile(lines[row][col]);
        }
    }

    return tiles;

    static Tile ParseTile(char c) => c switch
    {
        (char)Tile.Ground => Tile.Ground,
        (char)Tile.Start => Tile.Start,
        (char)Tile.NS => Tile.NS,
        (char)Tile.EW => Tile.EW,
        (char)Tile.NE => Tile.NE,
        (char)Tile.NW => Tile.NW,
        (char)Tile.SW => Tile.SW,
        (char)Tile.SE => Tile.SE,
        _ => throw new Exception($"unrecognized tile: {c}"),
    };
}

static (int row, int col) FindStart(Tile[,] tiles)
{
    var (height, width) = (tiles.GetLength(0), tiles.GetLength(1));

    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            if (tiles[row, col] == Tile.Start)
            {
                return (row, col);
            }
        }
    }
    throw new Exception("could not find start tile");
}

static int?[,] Search(Tile[,] tiles, (int row, int col) start)
{
    var (height, width) = (tiles.GetLength(0), tiles.GetLength(1));

    var depths = new int?[height, width];

    var frontier = new PriorityQueue<(int row, int col), int>(); // TODO: we're doing breadth first so this could be a plain queue
    var seen = new HashSet<(int row, int col)>();
    frontier.Enqueue(start, 0);

    while (frontier.TryDequeue(out (int, int) next, out int depth))
    {
        seen.Add(next);

        var (row, col) = next;

        depths[row, col] = depth;

        var tile = tiles[row, col];

        var neighbours = ConnectedNeighbours(next, tiles);
#if DEBUG
        Console.WriteLine($"{next}: neighbours are {string.Join(", ", neighbours)}");
#endif

        foreach (var neighbour in neighbours)
        {
            if (!seen.Contains(neighbour))
            {
                frontier.Enqueue(neighbour, depth + 1);
            }
        }
    }

    return depths;

    static IEnumerable<(int row, int col)> ConnectedNeighbours((int, int) next, Tile[,] tiles)
    {
        var (height, width) = (tiles.GetLength(0), tiles.GetLength(1));
        var (row, col) = next;
        var tile = tiles[row, col];

        if (row != 0 && (tile == Tile.Start && ConnectsSouth(tiles[row - 1, col]) || ConnectsNorth(tile)))
        {
            yield return (row - 1, col); // north
        }
        if (row < height - 1 && (tile == Tile.Start && ConnectsNorth(tiles[row + 1, col]) || ConnectsSouth(tile)))
        {
            yield return (row + 1, col); // south
        }
        if (col < width - 1 && (tile == Tile.Start && ConnectsWest(tiles[row, col + 1]) || ConnectsEast(tile)))
        {
            yield return (row, col + 1); // east
        }
        if (col != 0 && (tile == Tile.Start && ConnectsEast(tiles[row, col - 1]) || ConnectsWest(tile)))
        {
            yield return (row, col - 1); // west
        }

        static bool ConnectsNorth(Tile tile) => tile == Tile.NS || tile == Tile.NW || tile == Tile.NE;
        static bool ConnectsSouth(Tile tile) => tile == Tile.SE || tile == Tile.SW || tile == Tile.NS;
        static bool ConnectsEast(Tile tile) => tile == Tile.SE || tile == Tile.NE || tile == Tile.EW;
        static bool ConnectsWest(Tile tile) => tile == Tile.SW || tile == Tile.NW || tile == Tile.EW;
    }
}

static int MaxDepth(int?[,] depths)
{
    int max = int.MinValue;

    var (height, width) = (depths.GetLength(0), depths.GetLength(1));

    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            int? depth = depths[row, col];
            if (depth != null)
            {
                max = Math.Max(max, (int)depth);
            }
        }
    }

    return max;
}

static void PrintTileGrid(Tile[,] tiles) => PrintGrid(tiles, (rowcol) => { var (row, col) = rowcol; return (char)tiles[row, col]; });

static void PrintDepthGrid(int?[,] depths) => PrintGrid(depths, (rowcol) => { var (row, col) = rowcol; return DepthChar(depths[row, col]); });

static char DepthChar(int? depth) => depth switch
{
    null => '.',
    >= 0 and <= 9 => (char)('0' + depth),
    >= 10 and <= 35 => (char)('a' + depth),
    >= 36 and <= 59 => (char)('A' + depth),
    _ => '!',
};

static void PrintGrid<T>(T[,] grid, Func<(int, int), char> printfunc)
{
    var (height, width) = (grid.GetLength(0), grid.GetLength(1));

    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            char c = printfunc((row, col));
            Console.Write(c);
        }
        Console.WriteLine();
    }
}

enum Tile
{
    Ground = '.',
    Start = 'S',
    NS = '|',
    EW = '-',
    NE = 'L',
    NW = 'J',
    SW = '7',
    SE = 'F',
}
