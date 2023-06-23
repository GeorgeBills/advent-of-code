using System.Diagnostics;

string file = args.Length == 1 ? args[0] : "eg.txt";

var lines = File.ReadAllLines(file).ToList();
int splitIndex = lines.IndexOf("");

// parse in the map
var mapLines = lines[..splitIndex];
var map = ParseMap(mapLines);

// parse in the path
string pathLine = lines[splitIndex..].Where(line => !String.IsNullOrEmpty(line)).Single();
var path = ParsePath(pathLine);

// follow path
var position = GetStartingPosition(map);
Console.WriteLine($"the starting position is {position}");

#if DEBUG
PrintMap(map, position);
#endif

foreach (var p in path)
{
    position = p switch
    {
        int steps => position.WithStep(map, steps),
        Turn turn => position.WithTurn(turn),
    };
    Console.WriteLine($"{p} => {position}");

#if DEBUG
    PrintMap(map, position);
#endif
}

(int row, int column, var facing) = position;

// "The final password is the sum of 1000 times the row, 4 times the column, and
// the facing."
int password =
    1000 * (row + 1) +
    4 * (column + 1) +
    // "Facing is 0 for right (>), 1 for down (v), 2 for left (<), and 3 for up (^)."
    facing switch { Facing.Right => 0, Facing.Down => 1, Facing.Left => 2, Facing.Up => 3 };

Console.WriteLine($"the final position is {position}; password: {password}");










static Tile[,] ParseMap(List<string> mapLines)
{
    char[][] mapChars = mapLines
        .Select(line => line.ToCharArray())
        .ToArray();

    int width = mapLines.Select(line => line.Length).Max();
    int height = mapLines.Count();

    var map = new Tile[height, width];
    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            map[i, j] = j >= mapLines[i].Length
                ? Tile.Blank
                : mapLines[i][j] switch
                {
                    ' ' => Tile.Blank,
                    '#' => Tile.Wall,
                    '.' => Tile.Open,
                };
        }
    }

    return map;
}

static void PrintMap(Tile[,] map, Position? current = null)
{
    Console.Write(' ');

    for (int col = 0; col < map.GetLength(1); col++)
    {
        Console.Write(col % 10);
    }
    Console.WriteLine();

    for (int row = 0; row < map.GetLength(0); row++)
    {
        Console.Write(row % 10);
        for (int col = 0; col < map.GetLength(1); col++)
        {
            char draw = current != null && current.Row == row && current.Column == col
                ? (char)current.Facing
                : (char)map[row, col];

            Console.Write(draw);
        }
        Console.WriteLine();
    }
}

static IEnumerable<object> ParsePath(string pathLine)
{
    var path = new List<object>();

    var r = new StringReader(pathLine);

    int c = r.Peek();
    while (c != -1 && c != '\n' && c != '\r')
    {
        object p = c switch
        {
            'R' or 'L' => ParseTurn(r),
            >= '0' and <= '9' => ParseSteps(r),
        };
        path.Add(p);
        c = r.Peek();
    }

    return path.ToArray();

    Turn ParseTurn(TextReader r)
    {
        char c = (char)r.Read();
        return c switch
        {
            'R' => Turn.Right,
            'L' => Turn.Left,
        };
    }

    int ParseSteps(TextReader r)
    {
        int steps;
        int c;

        c = r.Read();
        if (c == -1 || !char.IsDigit((char)c))
        {
            throw new Exception($"unexpected non-digit: {c}");
        }
        steps = c - '0';

        c = r.Peek();
        while (char.IsDigit((char)c))
        {
            r.Read();
            steps = 10 * steps + c - '0';
            c = (char)r.Peek();
        }

        return steps;
    }
}

static Position GetStartingPosition(Tile[,] map)
{
    const int y = 0;
    for (int x = 0; x < map.GetLength(1); x++)
    {
        // "You begin the path in the leftmost open tile of the top row of
        // tiles. Initially, you are facing to the right(from the perspective of
        // how the map is drawn)."
        if (map[y, x] == Tile.Open)
        {
            return new Position(y, x, Facing.Right);
        }
    }
    throw new Exception("couldn't find starting position");
}

enum Tile { Blank = ' ', Open = '.', Wall = '#' };

enum Turn { Right = 'R', Left = 'L' };

enum Facing { Up = '^', Down = 'v', Right = '>', Left = '<' };

record Position(int Row, int Column, Facing Facing)
{
    public Position WithStep(Tile[,] map, int steps)
    {
        int height = map.GetLength(0);
        int width = map.GetLength(1);

        var position = this;
        while (steps > 0)
        {
            var prospective = step(position);

            var tile = map[prospective.Row, prospective.Column];
            while (tile == Tile.Blank)
            {
                // "If a movement instruction would take you off of the map, you
                // wrap around to the other side of the board."
                prospective = step(prospective);
                tile = map[prospective.Row, prospective.Column];
            }

            if (tile == Tile.Wall)
            {
                // "If you run into a wall, you stop moving forward and continue
                // with the next instruction."
                break;
            }

            // must be Tile.Open
            position = prospective;

            steps--;
        }

        return position;

        Position step(Position pos) => Facing switch
        {
            Facing.Up when pos.Row == 0 => pos with { Row = height - 1 },
            Facing.Up => pos with { Row = pos.Row - 1 },

            Facing.Down when pos.Row == height - 1 => pos with { Row = 0 },
            Facing.Down => pos with { Row = pos.Row + 1 },

            Facing.Right when pos.Column == width - 1 => pos with { Column = 0 },
            Facing.Right => pos with { Column = pos.Column + 1 },

            Facing.Left when pos.Column == 0 => pos with { Column = width - 1 },
            Facing.Left => pos with { Column = pos.Column - 1 },
        };
    }

    public Position WithTurn(Turn turn) => this with { Facing = newFacing(turn) };

    private Facing newFacing(Turn turn) => (Facing, turn) switch
    {
        (Facing.Up, Turn.Left) => Facing.Left,
        (Facing.Up, Turn.Right) => Facing.Right,
        (Facing.Down, Turn.Left) => Facing.Right,
        (Facing.Down, Turn.Right) => Facing.Left,
        (Facing.Right, Turn.Left) => Facing.Up,
        (Facing.Right, Turn.Right) => Facing.Down,
        (Facing.Left, Turn.Left) => Facing.Down,
        (Facing.Left, Turn.Right) => Facing.Up,
    };
}
