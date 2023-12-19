
using System.Data;

const string file = "in.txt";

var contraption = ParseContraption(file);

var (height, width) = (contraption.GetLength(0), contraption.GetLength(1));

var rows = Enumerable.Range(0, height);
var columns = Enumerable.Range(0, height);

var top = columns.Select(col => new Position(0, col)).Select(p => new Beam(Direction.S, p));
var bottom = columns.Select(col => new Position(height - 1, col)).Select(p => new Beam(Direction.N, p));
var left = rows.Select(row => new Position(row, 0)).Select(p => new Beam(Direction.E, p));
var right = rows.Select(row => new Position(row, width - 1)).Select(p => new Beam(Direction.W, p));

var starts = Enumerable.Empty<Beam>().Concat(top).Concat(bottom).Concat(left).Concat(right);

var max = starts
    .Select(b => (Beam: b, Energised: Simulate(contraption, b)))
    .MaxBy(be => be.Energised);

Console.WriteLine(max);

static void PrintEnergised(char[,] contraption, IEnumerable<Beam> energised)
{
    var (height, width) = (contraption.GetLength(0), contraption.GetLength(1));
    var positions = energised.Select(b => b.Position).ToHashSet();
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            var p = new Position(row, col);
            char c = positions.Contains(p) ? '#' : contraption[row, col];
            Console.Write(c);
        }
        Console.WriteLine();
    }
}

static char[,] ParseContraption(string file)
{
    string[] lines = File.ReadAllLines(file);
    int height = lines.Length;
    int width = lines.Select(l => l.Length).Distinct().Single();
    var contraption = new char[height, width];
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            contraption[row, col] = lines[row][col];
        }
    }
    return contraption;
}

static int Simulate(char[,] contraption, Beam start)
{
    var (height, width) = (contraption.GetLength(0), contraption.GetLength(1));

    var beams = new Queue<Beam>();
    beams.Enqueue(start);

    var energised = new HashSet<Beam> { start };

    while (beams.TryDequeue(out var beam))
    {
        // #if DEBUG
        //         Console.WriteLine(beam);
        //         PrintEnergised(contraption, energised);
        // #endif

        char square = contraption[beam.Position.Row, beam.Position.Column];
        var next = beam.Next(square);
        foreach (var n in next)
        {
            if (!n.Position.IsInBounds(height - 1, width - 1))
            {
                continue; // out of bounds
            }

            if (!energised.Add(n))
            {
                continue; // already processed
            }

            beams.Enqueue(n);
        }
    }

    return energised.Select(e => e.Position).Distinct().Count();
}

enum Direction { N, S, E, W };

record Beam(Direction Direction, Position Position)
{
    const char empty = '.';
    const char mirrorNWSE = '\\';
    const char mirrorSWNE = '/';
    const char splitterV = '|';
    const char splitterH = '-';

    public IEnumerable<Beam> Next(char square)
    {

        switch (square)
        {
            case empty:
                yield return NextStraight();
                break;
            case mirrorNWSE:
                yield return NextNWSE();
                break;
            case mirrorSWNE:
                yield return NextSWNE();
                break;
            case splitterH:
                if (Direction == Direction.E || Direction == Direction.W)
                {
                    yield return NextStraight();
                    break;
                }
                var (e, w) = NextSplitHorizontal();
                yield return e;
                yield return w;
                break;
            case splitterV:
                if (Direction == Direction.N || Direction == Direction.S)
                {
                    yield return NextStraight();
                    break;
                }
                var (n, s) = NextSplitVertical();
                yield return n;
                yield return s;
                break;
        }
    }

    Beam NextStraight() => Direction switch
    {
        Direction.N => this with { Position = Position with { Row = Position.Row - 1 } },
        Direction.S => this with { Position = Position with { Row = Position.Row + 1 } },
        Direction.E => this with { Position = Position with { Column = Position.Column + 1 } },
        Direction.W => this with { Position = Position with { Column = Position.Column - 1 } },
    };

    Beam NextNWSE() => Direction switch // the "\" type of mirror
    {
        Direction.N => this with { Direction = Direction.W, Position = Position with { Column = Position.Column - 1 } },
        Direction.S => this with { Direction = Direction.E, Position = Position with { Column = Position.Column + 1 } },
        Direction.E => this with { Direction = Direction.S, Position = Position with { Row = Position.Row + 1 } },
        Direction.W => this with { Direction = Direction.N, Position = Position with { Row = Position.Row - 1 } },
    };

    Beam NextSWNE() => Direction switch // the "/" type of mirror      
    {
        Direction.N => this with { Direction = Direction.E, Position = Position with { Column = Position.Column + 1 } },
        Direction.S => this with { Direction = Direction.W, Position = Position with { Column = Position.Column - 1 } },
        Direction.E => this with { Direction = Direction.N, Position = Position with { Row = Position.Row - 1 } },
        Direction.W => this with { Direction = Direction.S, Position = Position with { Row = Position.Row + 1 } },
    };

    (Beam, Beam) NextSplitVertical() => (
        this with { Direction = Direction.N, Position = Position with { Row = Position.Row - 1 } },
        this with { Direction = Direction.S, Position = Position with { Row = Position.Row + 1 } }
    );

    (Beam, Beam) NextSplitHorizontal() => (
        this with { Direction = Direction.E, Position = Position with { Column = Position.Column + 1 } },
        this with { Direction = Direction.W, Position = Position with { Column = Position.Column - 1 } }
    );
}

record Position(int Row, int Column)
{
    public bool IsInBounds(int MaxRow, int MaxColumn) => 0 <= Row && Row <= MaxRow && 0 <= Column && Column <= MaxColumn;
}
