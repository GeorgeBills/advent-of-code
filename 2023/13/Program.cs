const string file = "in.txt";

const char ash = '.';
const char rock = '#';

var grids = ParsePatternGrids(file);
int score = grids.Select(ReflectionSummary).Sum();
Console.WriteLine(score);

static int? FindHorizontalReflection(char[,] grid)
{
    int height = grid.GetLength(0);
    for (int i = 0; i < height - 1; i++)
    {
        // Console.WriteLine($"is horizontal? {(i, i + 1)}");
        if (IsHorizontalReflection(grid, i, i + 1))
        {
            return i;
        }
    }
    return null;
}

static int? FindVerticalReflection(char[,] grid)
{
    int width = grid.GetLength(1);
    for (int i = 0; i < width - 1; i++)
    {
        // Console.WriteLine($"is vertical? {(i, i + 1)}");
        if (IsVerticalReflection(grid, i, i + 1))
        {
            return i;
        }
    }
    return null;
}

static bool IsHorizontalReflection(char[,] grid, int i, int j) =>
        Enumerable.SequenceEqual(Row(grid, i), Row(grid, j)) &&
        ((i < 1) /* the ith row is the first row */ ||
         (j >= grid.GetLength(0) - 1) /* the jth row is the last row */ ||
         IsHorizontalReflection(grid, i - 1, j + 1));

static bool IsVerticalReflection(char[,] grid, int i, int j) =>
        Enumerable.SequenceEqual(Column(grid, i), Column(grid, j)) &&
        ((i < 1) /* the ith column is the first column */ ||
         (j >= grid.GetLength(1) - 1) /* the jth column is the last column */ ||
         IsVerticalReflection(grid, i - 1, j + 1));

static IEnumerable<int> RowIndexes(char[,] grid) => Enumerable.Range(0, grid.GetLength(0) - 1);

static IEnumerable<int> ColumnIndexes(char[,] grid) => Enumerable.Range(0, grid.GetLength(1) - 1);

static IEnumerable<char> Row(char[,] grid, int row)
{
    if (row < 0 || row >= grid.GetLength(0))
    {
        throw new ArgumentOutOfRangeException(nameof(row), $"row {row} is out of range {0}..{grid.GetLength(0) - 1}");
    }
    return ColumnIndexes(grid).Select(col => grid[row, col]);
}

static IEnumerable<char> Column(char[,] grid, int col)
{
    if (col < 0 || col >= grid.GetLength(1))
    {
        throw new ArgumentOutOfRangeException(nameof(col), $"column {col} is out of range {0}..{grid.GetLength(1) - 1}");
    }
    return RowIndexes(grid).Select(row => grid[row, col]);
}

static char[][,] ParsePatternGrids(string file)
{
    var input = File.ReadLines(file);
    var grids = new List<char[,]>();

    while (input.Any())
    {
        char[,] grid;
        (grid, input) = ParsePatternGrid(input);

// #if DEBUG
//         PrintPatternGrid(grid);
//         Console.WriteLine();
// #endif

        grids.Add(grid);
    }

    return grids.ToArray();
}
static (char[,], IEnumerable<string>) ParsePatternGrid(IEnumerable<string> input)
{
    var lines = input.TakeWhile(l => l.Length > 0).ToArray();

    int height = lines.Length;
    int width = lines.Select(l => l.Length).Distinct().Single();

    var grid = new char[height, width];
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            grid[row, col] = lines[row][col];
        }
    }

    input = input.Skip(height).SkipWhile(l => l.Length == 0);

    return (grid, input);
}

static void PrintPatternGrid(char[,] grid)
{
    var (height, width) = (grid.GetLength(0), grid.GetLength(1));
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            Console.Write(grid[row, col]);
        }
        Console.WriteLine();
    }
}

static int ReflectionSummary(char[,] grid)
{
    int score = 0;

    int? khor = FindHorizontalReflection(grid);
    if (khor != null)
    {
        // "100 multiplied by the number of rows above each horizontal line of reflection"
        score += 100 * ((int)khor + 1);
        // Console.WriteLine($"pattern reflects horizontally on {khor}:{khor + 1} for a score of {score}");
    }

    int? kver = FindVerticalReflection(grid);
    if (kver != null)
    {
        // "number of columns to the left of each vertical line of reflection"
        score += (int)kver + 1;
        // Console.WriteLine($"pattern reflects vertically on {kver}:{kver + 1} for a score of {score}");
    }

    return score;
}
