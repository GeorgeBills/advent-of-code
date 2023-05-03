if (args.Length != 1)
{
    Console.WriteLine("require input file");
    Environment.Exit(1);
}

var lines = File.ReadAllLines(args[0]);

(int[,] trees, var dimensions) = ParseLines(lines);
PrintTrees(trees, dimensions);

bool[,] visible = CalculateVisible(trees, dimensions);
PrintVisible(visible, dimensions);

int numVisible = visible.Cast<bool>().Count(v => v);
Console.WriteLine($"{numVisible} trees visible");

(int[,] scores, (int maxI, int maxJ, int maxScore)) = CalculateScenicScores(trees, dimensions);
Console.WriteLine($"most scenic tree is at {maxI},{maxJ} and scored {maxScore}");

(int[,], Dimensions) ParseLines(string[] lines)
{
    int height = lines.Length;
    int width = lines.Select(line => line.Length).Distinct().Single();

    int[,] trees = new int[height, width];

    for (int i = 0; i < lines.Length; i++)
    {
        char[] line = lines[i].ToCharArray();
        for (int j = 0; j < line.Length; j++)
        {
            int tree = line[j] - '0';
            trees[i, j] = tree;
        }
    }

    return (trees, new Dimensions(height: height, width: width));
}

(int i, int j) next(Direction dir, int i, int j) => dir switch
{
    Direction.North => (i - 1, j),
    Direction.South => (i + 1, j),
    Direction.East => (i, j + 1),
    Direction.West => (i, j - 1),
    _ => throw new ArgumentException($"{dir} unhandled"),
};

bool[,] CalculateVisible(int[,] trees, Dimensions dimensions)
{
    (int height, int width) = dimensions;

    bool[,] visible = new bool[height, width];

    // corners
    visible[0, 0] = true;
    visible[0, width - 1] = true;
    visible[height - 1, 0] = true;
    visible[height - 1, width - 1] = true;

    // top
    for (int j = 1; j < width - 1; j++)
    {
        int i = 0;
        visible[i, j] = true;
        scanVisible(Direction.South, i, j);
    }

    // bottom
    for (int j = 1; j < width - 1; j++)
    {
        int i = height - 1;
        visible[i, j] = true;
        scanVisible(Direction.North, i, j);
    }

    // left
    for (int i = 1; i < height - 1; i++)
    {
        int j = 0;
        visible[i, j] = true;
        scanVisible(Direction.East, i, j);
    }

    // right
    for (int i = 1; i < height - 1; i++)
    {
        int j = height - 1;
        visible[i, j] = true;
        scanVisible(Direction.West, i, j);
    }

    return visible;

    void scanVisible(Direction dir, int i, int j)
    {
        // Console.WriteLine($"scan visible: {new { dir, i, j }}");
        int max = trees[i, j];
        for ((i, j) = next(dir, i, j); 0 <= i && i < height && 0 <= j && j < width; (i, j) = next(dir, i, j))
        {
            // Console.WriteLine($"{i},{j}");
            int height = trees[i, j];
            if (height > max)
            {
                // Console.WriteLine($"tree at {i},{j} is visible: {height} > {max}");
                max = height;
                visible[i, j] = true;
            }
        }
    };
}

(int[,] scores, (int i, int j, int score) max) CalculateScenicScores(int[,] trees, Dimensions dimensions)
{
    (int height, int width) = dimensions;

    int[,] scores = new int[width, height];

    (int i, int j, int score) max = (-1, -1, -1);
    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            int scoreN = scanViewingDistance(Direction.North, i, j);
            int scoreS = scanViewingDistance(Direction.South, i, j);
            int scoreE = scanViewingDistance(Direction.East, i, j);
            int scoreW = scanViewingDistance(Direction.West, i, j);
            int score = scoreN * scoreS * scoreE * scoreW;

            // Console.WriteLine($"{i},{j}: N {scoreN} x W {scoreW} x E {scoreE} x S {scoreS} = {score}");

            scores[i, j] = score;

            if (score > max.score)
            {
                max = (i, j, score);
            }
        }
    }

    return (scores, max);

    int scanViewingDistance(Direction dir, int i, int j)
    {
        int score = 0;

        int max = trees[i, j];
        for ((i, j) = next(dir, i, j); 0 <= i && i < height && 0 <= j && j < width; (i, j) = next(dir, i, j))
        {
            score++;

            int height = trees[i, j];
            if (height >= max)
            {
                break;
            }
        }

        return score;
    };
}

void PrintVisible(bool[,] visible, Dimensions dimensions)
{
    Print(visible, dimensions, (bool b) => b ? 'x' : ' ');
    Console.WriteLine();
}

void PrintTrees(int[,] trees, Dimensions dimensions)
{
    Print(trees, dimensions, (int n) => (char)('0' + n));
    Console.WriteLine();
}

void Print<T>(T[,] arr, Dimensions dimensions, Func<T, char> toPrintable)
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

record Dimensions(int height, int width);
