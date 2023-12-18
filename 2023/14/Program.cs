using System.Text;

const string file = "in.txt";

const char rounded = 'O';
const char cubical = '#';
const char empty = '.';

var platform = ParsePlatform(file);
PrintPlatform(platform);

var (height, width) = (platform.GetLength(0), platform.GetLength(1));

// get the columns with the rounded rocks rolled as far north as possible
var columns = Enumerable.Range(0, width)
    .Select(colidx => Column(platform, colidx))
    .Select(
        colstr =>
            colstr.Split(cubical)
                .Select(CountRocks)
                .Select(n => new string(rounded, n.Rounded) + new string(empty, n.Empty))
    )
    .Select(colarr => string.Join(cubical, colarr));

int weight = columns
    .SelectMany(colstr =>
        colstr
        .ToCharArray()
        .Select((rock, idx) => rock == rounded ? height - idx : 0)
    )
    .Sum();

Console.WriteLine(weight);

static RockCount CountRocks(string s) =>
    s.Aggregate(
        new RockCount(),
        (rc, ch) => ch switch
        {
            rounded => rc with { Rounded = rc.Rounded + 1 },
            empty => rc with { Empty = rc.Empty + 1 },
        }
    );

static string Column(char[,] platform, int column)
{
    var sb = new StringBuilder();
    int height = platform.GetLength(0);
    for (int row = 0; row < height; row++)
    {
        sb.Append(platform[row, column]);
    }
    return sb.ToString();
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

record RockCount(int Rounded = 0, int Empty = 0);
