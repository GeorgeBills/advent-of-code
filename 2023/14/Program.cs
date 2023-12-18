using System.Diagnostics;

const string file = "eg.txt";

const char rounded = 'O';
const char cubical = '#';
const char empty = '.';

const int directions = 4; // north to south, south to north, east to west, west to east

var platform = ParsePlatform(file);

var (height, width) = (platform.GetLength(0), platform.GetLength(1));

var rows = Enumerable.Range(0, height);
var cols = Enumerable.Range(0, width);

// "north, then west, then south, then east"
var indexes = new IEnumerable<IEnumerable<(int Row, int Column)>>[directions] {
    cols.Select(col => rows.Select(row => (row, col))),           // columns north to south
    rows.Select(row => cols.Select(col => (row, col))),           // rows west to east
    cols.Select(col => rows.Reverse().Select(row => (row, col))), // columns south to north
    rows.Select(row => cols.Reverse().Select(col => (row, col))), // rows east to west
};

// 4 directions to N ranges to M index tuples
var ranges = indexes
    .Select(
        i => i
            .SelectMany(indexes => SplitCubical(platform, indexes))
            .Select(range => range.ToArray())
            .Where(range => range.Length > 1) // rocks can't roll in a single space
            .ToArray()
    )
    .ToArray();

#if DEBUG
Console.WriteLine("initial state");
PrintPlatform(platform);
Console.WriteLine();
#endif

const int iterations = 1_000_000_000;
var sw = Stopwatch.StartNew();
var seen = new HashSet<char[,]>();
for (int i = 0; i < iterations; i++)
{
#if DEBUG
    Console.WriteLine($"iteration {i + 1}");
#endif

    for (int j = 0; j < directions; j++)
    {
        Roll(platform, ranges[j]);
#if DEBUG
        Console.WriteLine($"rolling in direction {j + 1}");
        PrintPlatform(platform);
        Console.WriteLine();
#endif
    }

    if (seen.Contains(platform))
    {
        // FIXME: need a dictionary(?) of state to weight
        //        state we repeat on will not be the state we terminate on
        Console.WriteLine($"exiting early: repeat observed after iteration {i + 1} ({sw.Elapsed})");
        break;
    }
    seen.Add(platform);
}

int weight = RoundedRocks(platform)
    .Select(rc => height - rc.Row)
    .Sum();

Console.WriteLine($"total weight on the north support beams is {weight}");

static IEnumerable<IEnumerable<(int Row, int Column)>> SplitCubical(char[,] platform, IEnumerable<(int Row, int Column)> indexes)
{
    var range = new List<(int, int)>();
    foreach (var (row, col) in indexes)
    {
        if (platform[row, col] != cubical)
        {
            range.Add((row, col));
        }
        else if (range.Count > 0)
        {
            yield return range;
            range = new List<(int, int)>();
        }
    }
    if (range.Count > 0)
    {
        yield return range;
    }
}

static void Roll(char[,] platform, (int Row, int Column)[][] ranges)
{
    foreach (var range in ranges)
    {
        // count the number of rounded rocks in this range
        int n = range.Count(rc => platform[rc.Row, rc.Column] == rounded);

        // set the first n positions in the range to rounded rocks
        for (int i = 0; i < n; i++)
        {
            var (row, col) = range[i];
            platform[row, col] = rounded;
        }

        // set the remaining positions to empty
        for (int i = n; i < range.Length; i++)
        {
            var (row, col) = range[i];
            platform[row, col] = empty;
        }
    }
}

static IEnumerable<(int Row, int Column)> RoundedRocks(char[,] platform)
{
    var (height, width) = (platform.GetLength(0), platform.GetLength(1));
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            if (platform[row, col] == rounded)
            {
                yield return (row, col);
            }
        }
    }
}

static char[,] ParsePlatform(string file)
{
    var lines = File.ReadAllLines(file);
    int height = lines.Length;
    int width = lines.Select(l => l.Length).Distinct().Single();
    var platform = new char[height, width];
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            platform[row, col] = lines[row][col];
        }
    }
    return platform;
}

#if DEBUG
static void PrintPlatform(char[,] platform)
{
    var (height, width) = (platform.GetLength(0), platform.GetLength(1));
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            Console.Write(platform[row, col]);
        }
        Console.WriteLine();
    }
}
#endif
