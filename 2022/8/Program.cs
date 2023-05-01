if (args.Length != 1)
{
    Console.WriteLine("require input file");
    Environment.Exit(1);
}

var lines = File.ReadAllLines(args[0]);

(int[,] trees, bool[,] visible, var dimensions) = ParseLines(lines);
PrintTrees(trees, dimensions);
SetVisible(visible, dimensions);
PrintVisible(visible, dimensions);
int numVisible = visible.Cast<bool>().Count(v => v);
Console.WriteLine($"{numVisible} trees visible");

(int[,] trees, bool[,] visible, (int height, int width)) ParseLines(string[] lines)
{
    int height = lines.Length;
    int width = lines.Select(line => line.Length).Distinct().Single();

    int[,] trees = new int[height, width];
    bool[,] visible = new bool[height, width];

    for (int i = 0; i < lines.Length; i++)
    {
        char[] line = lines[i].ToCharArray();
        for (int j = 0; j < line.Length; j++)
        {
            int tree = line[j] - '0';
            trees[i, j] = tree;
        }
    }

    return (trees, visible, (height, width));
}

void SetVisible(bool[,] visible, (int height, int width) dimensions)
{
    (int i, int j) next(Direction dir, int i, int j) => dir switch
    {
        Direction.North => (i - 1, j),
        Direction.South => (i + 1, j),
        Direction.East => (i, j + 1),
        Direction.West => (i, j - 1),
        _ => throw new ArgumentException($"{dir} unhandled"),
    };

    void scanVisible(Direction dir, int i, int j)
    {
        // Console.WriteLine($"scan visible: {new { dir, i, j }}");
        int max = trees[i, j];
        for (; 0 <= i && i < dimensions.height && 0 <= j && j < dimensions.width; (i, j) = next(dir, i, j))
        {
            // Console.WriteLine(new { i, j });
            int height = trees[i, j];
            if (height > max)
            {
                // Console.WriteLine($"tree at {i},{j} is visible: {height} > {max}");
                max = height;
                visible[i, j] = true;
            }
        }
    };

    // corners
    visible[0, 0] = true;
    visible[0, dimensions.width - 1] = true;
    visible[dimensions.height - 1, 0] = true;
    visible[dimensions.height - 1, dimensions.width - 1] = true;

    // top
    for (int j = 1; j < dimensions.width - 1; j++)
    {
        int i = 0;
        visible[i, j] = true;
        scanVisible(Direction.South, i, j);
    }

    // bottom
    for (int j = 1; j < dimensions.width - 1; j++)
    {
        int i = dimensions.height - 1;
        visible[i, j] = true;
        scanVisible(Direction.North, i, j);
    }

    // left
    for (int i = 1; i < dimensions.height - 1; i++)
    {
        int j = 0;
        visible[i, j] = true;
        scanVisible(Direction.East, i, j);
    }

    // right
    for (int i = 1; i < dimensions.height - 1; i++)
    {
        int j = dimensions.height - 1;
        visible[i, j] = true;
        scanVisible(Direction.West, i, j);
    }
}

void PrintVisible(bool[,] visible, (int height, int width) dimensions)
{
    Print(visible, dimensions, (bool b) => b ? 'x' : ' ');
    Console.WriteLine();
}

void PrintTrees(int[,] trees, (int height, int width) dimensions)
{
    Print(trees, dimensions, (int n) => (char)('0' + n));
    Console.WriteLine();
}

void Print<T>(T[,] arr, (int height, int width) dimensions, Func<T, char> toPrintable)
{
    for (int i = 0; i < dimensions.height; i++)
    {
        for (int j = 0; j < dimensions.width; j++)
        {
            char c = toPrintable(arr[i, j]);
            Console.Write(c);
        }
        Console.WriteLine();
    }
}

enum Direction { North, South, East, West };
