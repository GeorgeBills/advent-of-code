string file = args.Length >= 1 ? args[0] : "eg.txt";

char[,] schematic = ParseGrid(file);

PrintGrid(schematic);

var numbers = new Dictionary<Coordinate, Number>();
var symbols = new HashSet<Coordinate>();

int n = 0, startCol = 0;
var previous = State.Blank;
for (int row = 0; row < schematic.GetLength(0); row++)
{
    for (int col = 0; col < schematic.GetLength(1); col++)
    {
        char c = schematic[row, col];

        // what is the current state we're dealing with?
        var current = c switch
        {
            '.' => State.Blank,
            var d when char.IsDigit(d) => State.Number,
            _ => State.Symbol,
        };

        if (current == State.Number) // a number
        {
            n = n * 10 + ToDigit(c);
            if (previous != State.Number)
            {
                // start of a number
                startCol = col;
            }
        }
        else if (previous == State.Number)
        {
            // end of a number; record it in our dictionary
            int endCol = col - 1;
            var num = new Number(n, row, startCol, endCol);
            for (int i = startCol; i <= endCol; i++)
            {
                var coord = new Coordinate(row, i);
                numbers[coord] = num;
            }
        }

        if (current == State.Symbol) // a symbol
        {
            var coord = new Coordinate(row, col);
            symbols.Add(coord);
        }

        if (current == State.Blank)
        {
            n = 0;
        }

        previous = current;
    }
    // end of a row: reset state
    n = 0;
    previous = State.Blank;
}

PrintNumbers(numbers);
PrintSymbols(symbols);

// get all of the coordinates that neighbour a symbol
var neighbours = symbols.SelectMany(coord => coord.Neighbours(MaxRow: schematic.GetLength(0) - 1, MaxCol: schematic.GetLength(1) - 1)).Distinct();

// get all of the part numbers in the coordinates we care about and sum them
int answer = neighbours.Select(want => numbers.GetValueOrDefault(want)).Where(num => num != null).Cast<Number>().Distinct().Select(num => num.N).Sum();

Console.WriteLine($"the sum of the part numbers is {answer}");

static int ToDigit(char c) => c - '0';

static char[,] ParseGrid(string file)
{
    var lines = File.ReadAllLines(file);
    int height = lines.Length;
    int width = lines.Select(line => line.Length).Distinct().Single();
    var grid = new char[height, width];
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            grid[row, col] = lines[row][col];
        }
    }
    return grid;
}

static void PrintGrid(char[,] grid)
{
    for (int row = 0; row < grid.GetLength(0); row++)
    {
        for (int col = 0; col < grid.GetLength(1); col++)
        {
            Console.Write(grid[row, col]);
        }
        Console.WriteLine();
    }
}

static void PrintNumbers(Dictionary<Coordinate, Number> numbers)
{
    foreach (var kv in numbers)
    {
        Console.WriteLine(kv.ToString());
    }
}

static void PrintSymbols(HashSet<Coordinate> symbols)
{
    foreach (var coord in symbols)
    {
        Console.WriteLine(coord);
    }
}

enum State { Number, Symbol, Blank };

record Number(int N, int Row, int StartCol, int EndCol);

record Coordinate(int Row, int Col)
{
    public IEnumerable<Coordinate> Neighbours(int MaxRow, int MaxCol)
    {
        for (int row = Row - 1; row <= Row + 1; row++)
        {
            for (int col = Col - 1; col <= Col + 1; col++)
            {
                bool inBounds = 0 <= row && row <= MaxRow && 0 <= col && col <= MaxCol;
                if (inBounds)
                {
                    yield return new Coordinate(row, col);
                }
            }
        }
    }
}
