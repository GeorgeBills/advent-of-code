﻿string file = args.Length >= 1 ? args[0] : "eg.txt";

char[,] schematic = ParseGrid(file);

PrintGrid(schematic);

var numbers = new Dictionary<Coordinate, Number>();
var symbols = new Dictionary<Coordinate, char>();

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
            AddNumber(n, row, startCol, endCol: col - 1);
            n = 0;
        }

        if (current == State.Symbol) // a symbol
        {
            var coord = new Coordinate(row, col);
            symbols[coord] = c;
        }

        previous = current;
    }
    // end of a row: reset state
    if (previous == State.Number)
    {
        AddNumber(n, row, startCol, endCol: schematic.GetLength(1) - 1);
    }

    n = 0;
    previous = State.Blank;

    void AddNumber(int n, int row, int startCol, int endCol)
    {
        var num = new Number(n, row, startCol, endCol);
        for (int i = startCol; i <= endCol; i++)
        {
            var coord = new Coordinate(row, i);
            numbers[coord] = num;
        }
    }
}

PrintDictionary(numbers);
PrintNumbersGrid(numbers, height: schematic.GetLength(0), width: schematic.GetLength(1));
PrintDictionary(symbols);
PrintSymbolsGrid(symbols, height: schematic.GetLength(0), width: schematic.GetLength(1));

// part one
{
    // get all of the coordinates that neighbour a symbol
    var neighbours = symbols.Keys
        .SelectMany(coord => coord.Neighbours(MaxRow: schematic.GetLength(0) - 1, MaxCol: schematic.GetLength(1) - 1))
        .Distinct();

    // get all of the part numbers in the coordinates we care about and sum them
    var parts = neighbours
        .Select(coord => numbers.GetValueOrDefault(coord))
        .OfType<Number>()
        .Distinct()
        .Select(num => num.N);

    Console.WriteLine($"the sum of the part numbers is {parts.Sum()}");
}

// part two
{
    // get all of the "gear" (*) symbols
    var gears = symbols
        .Where(kv => kv.Value == '*')
        .Select(kv => kv.Key);

    Console.WriteLine($"there are {gears.Count()} gears");

    // calculate all of the gear ratios
    var ratios = gears
        // for each gear, get numbers adjacent to that gear
        .Select(coord =>
            coord
                // coordinates adjacent to a gear
                .Neighbours(MaxRow: schematic.GetLength(0) - 1, MaxCol: schematic.GetLength(1) - 1)
                // numbers covering one of those coordinates
                .Select(neighbour => numbers.GetValueOrDefault(neighbour))
                // filter out nulls (no number in the coordinate we checked)
                .OfType<Number>()
                // possible / probable that one number will cover many coordinates
                .Distinct()
        )
        // "...that is adjacent to exactly two part numbers"
        .Where(nums => nums.Count() == 2)
        // convert to an array (so we can index into it) of integers (so we can multiply)
        .Select(nums => nums.Select(num => num.N).ToArray())
        // "gear ratio is the result of multiplying those two numbers together"
        .Select(nums => nums[0] * nums[1]);

    Console.WriteLine($"the sum of all the gear ratios is {ratios.Sum()}");
}

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

static void PrintGridFunc(int height, int width, Func<Coordinate, char> printfunc)
{
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            var coord = new Coordinate(row, col);
            char c = printfunc(coord);
            Console.Write(c);
        }
        Console.WriteLine();
    }
}

static void PrintGrid(char[,] grid) => PrintGridFunc(height: grid.GetLength(0), width: grid.GetLength(1), coord => grid[coord.Row, coord.Col]);

static void PrintDictionary<T>(Dictionary<Coordinate, T> numbers)
{
    foreach (var kv in numbers)
    {
        Console.WriteLine(kv.ToString());
    }
}

static void PrintNumbersGrid(Dictionary<Coordinate, Number> numbers, int height, int width) => PrintGridFunc(height, width, coord => numbers.ContainsKey(coord) ? 'N' : '.');

static void PrintSymbolsGrid(Dictionary<Coordinate, char> symbols, int height, int width) => PrintGridFunc(height, width, coord => symbols.ContainsKey(coord) ? '#' : '.');

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
