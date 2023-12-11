using System.Diagnostics;

const string file = "in.txt";

var sw = Stopwatch.StartNew();

var (image, galaxies) = Parse(file);
#if DEBUG
PrintImage(image);
PrintGalaxies(galaxies);
#endif

var expanded = GetExpanded(image, galaxies);
#if DEBUG
PrintExpanded(expanded);
#endif

var depths = CalculateDepths(image, galaxies, expanded);
#if DEBUG
PrintDepths(galaxies, depths);
#endif

var pairs = Pairs(galaxies);

long total = 0;
foreach (var pair in pairs)
{
    var ((aidx, _), (bidx, (brow, bcol))) = pair;
    int cost = depths[aidx][brow, bcol];

#if DEBUG
    Console.WriteLine($"path cost from galaxy {aidx + 1} to galaxy {bidx + 1} is {cost}");
#endif

    total += cost;
}

sw.Stop();

Console.WriteLine($"total cost for all pairs is {total} (took {sw.Elapsed})");

static (char[,] image, (int Row, int Column)[] galaxies) Parse(string file)
{
    string[] lines = File.ReadAllLines(file);

    int height = lines.Length;
    int width = lines.Select(l => l.Length).Distinct().Single();

    var image = new char[height, width];
    var galaxies = new List<(int, int)>();
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            char c = lines[row][col];
            if (c == '#')
            {
                galaxies.Add((row, col));
            }
            image[row, col] = c;
        }
    }

    return (image, galaxies.ToArray());
}

static bool[,] GetExpanded(char[,] image, (int Row, int Column)[] galaxies)
{
    var (height, width) = (image.GetLength(0), image.GetLength(1));

    var expanded = new bool[height, width];

    foreach (int row in Enumerable.Range(0, height).Except(galaxies.Select(g => g.Row)))
    {
        for (int col = 0; col < width; col++)
        {
            expanded[row, col] = true;
        }
    }

    foreach (int col in Enumerable.Range(0, width).Except(galaxies.Select(g => g.Column)))
    {
        for (int row = 0; row < width; row++)
        {
            expanded[row, col] = true;
        }
    }

    return expanded;
}

static int[][,] CalculateDepths(char[,] image, (int Row, int Column)[] galaxies, bool[,] expanded)
{
    var (height, width) = (image.GetLength(0), image.GetLength(1));

    var depths = new int[galaxies.Length][,];
    for (int g = 0; g < depths.Length; g++)
    {
        const int unexplored = int.MaxValue;
        const int expandedcost = 1_000_000; // "make each empty row or column one million times larger"

#if DEBUG
        Console.WriteLine($"calculating depth for galaxy {g + 1} @ {galaxies[g]}");
#endif

        depths[g] = new int[height, width];
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                depths[g][row, col] = unexplored;
            }
        }

        var frontier = new PriorityQueue<(int, int), int>();
        frontier.Enqueue(galaxies[g], 0); // initial depth of 0

        while (frontier.TryDequeue(out var pos, out int depth))
        {
            var (row, col) = pos;

            if (depths[g][row, col] != unexplored)
            {
                continue;
            }

            depths[g][row, col] = depth;

            var neighbours = OrthogonalNeighbours(row, col);
            foreach (var neighbour in neighbours)
            {
                var (nrow, ncol) = neighbour;
                int cost = expanded[nrow, ncol] ? expandedcost : 1;
                frontier.Enqueue(neighbour, depth + cost);
            }
        }
    }

    return depths;

    IEnumerable<(int Row, int Column)> OrthogonalNeighbours(int row, int col)
    {
        if (row > 0) { yield return (row - 1, col); }
        if (col > 0) { yield return (row, col - 1); }
        if (row < height - 1) { yield return (row + 1, col); }
        if (col < width - 1) { yield return (row, col + 1); }
    }
}

static IEnumerable<((int Index, T Value), (int Index, T Value))> Pairs<T>(T[] values)
{
    for (int i = 0; i < values.Length; i++)
    {
        for (int j = i + 1; j < values.Length; j++)
        {
            yield return ((i, values[i]), (j, values[j]));
        }
    }
}

#if DEBUG
static void PrintGrid<T>(T[,] grid, Func<T, char> print)
{
    var (height, width) = (grid.GetLength(0), grid.GetLength(1));

    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            char c = print(grid[i, j]);
            Console.Write(c);
        }
        Console.WriteLine();
    }
}
#endif

#if DEBUG
static void PrintImage(char[,] image) => PrintGrid(image, c => c);
#endif

#if DEBUG
static void PrintGalaxies((int, int)[] galaxies) =>
    Console.WriteLine(string.Join("; ", galaxies.Select((g, i) => $"{i + 1}: {g}")));
#endif

#if DEBUG
static void PrintExpanded(bool[,] expanded) => PrintGrid(expanded, e => e ? 'x' : '.');
#endif

#if DEBUG
static void PrintDepths((int Row, int Column)[] galaxies, int[][,] depths)
{
    for (int g = 0; g < galaxies.Length; g++)
    {
        Console.WriteLine($"galaxy {g} @ {galaxies[g]}:");
        PrintGrid(depths[g], DepthChar);
    }
    static char DepthChar(int d) => d switch
    {
        >= 0 and <= 9 => (char)('0' + d),
        >= 10 and <= 35 => (char)('a' + d - 10),
        >= 36 and <= 61 => (char)('A' + d - 36),
        _ => '!',
    };
}
#endif
