string file = args.Length == 1 ? args[0] : "egsmall.txt";

char[][] diagram = File.ReadAllLines(file).Select(line => line.ToCharArray()).ToArray();
var elves = GetElves(diagram).ToHashSet();

#if DEBUG
PrintElves(elves);
PrintElvesDiagram(elves);
#endif

int round;
const int rounds = 10;
for (round = 1; round <= rounds; round++)
{
#if DEBUG
    Console.WriteLine($"== round {round} ==");
#endif

    var proposals = elves
        .Select(elf => (elf, dir: Proposer.Propose(elf, elves, round - 1)))          // ask all the elves to propose a move
        .Where(elfdir => elfdir.dir != null)                                         // filter out elves who don't propose a move
        .Select(elfdir => (oldpos: elfdir.elf, newpos: elfdir.elf.Step(elfdir.dir))) // calculate positions elves are proposing to move to
        .GroupBy(proposal => proposal.newpos).Where(group => group.Count() == 1)     // "if they were the only Elf to propose moving to that position"
        .Select(group => group.Single());

    if (proposals.Count() == 0)
    {
        Console.WriteLine($"finishing: no elves are proposing a move after {round} rounds");
        break;
    }

    foreach (var proposal in proposals)
    {
        // Console.WriteLine($"{proposal.oldpos} => {proposal.newpos}");
        elves.Remove(proposal.oldpos);
        elves.Add(proposal.newpos);
    }

#if DEBUG
    PrintElvesDiagram(elves);
#endif
}

int empty = CountEmpty(elves);

Console.WriteLine($"there are {empty} empty ground tiles after {round} rounds");

static IEnumerable<Position> GetElves(char[][] diagram)
{
    int height = diagram.Length;
    int width = diagram.Select(row => row.Length).Distinct().Single();
    for (int row = 0; row < height; row++)
    {
        for (int col = 0; col < width; col++)
        {
            const char elf = '#';
            if (diagram[row][col] == elf)
            {
                yield return new Position(row, col);
            }
        }
    }
}

static (int minRow, int maxRow, int minCol, int maxCol) GetBounding(IEnumerable<Position> elves) =>
    elves.Aggregate(
        (minRow: int.MaxValue,
         maxRow: int.MinValue,
         minCol: int.MaxValue,
         maxCol: int.MinValue),
        (acc, elf) => (
            minRow: Math.Min(elf.Row, acc.minRow),
            maxRow: Math.Max(elf.Row, acc.maxRow),
            minCol: Math.Min(elf.Column, acc.minCol),
            maxCol: Math.Max(elf.Column, acc.maxCol)
        )
    );

const int maxPrintElves = 25;
static void PrintElves(ISet<Position> elves) =>
    Console.WriteLine(
        String.Join(
            ", ",
            elves.OrderBy(e => e.Row).ThenBy(e => e.Column).Take(maxPrintElves)) + (elves.Count() > maxPrintElves ? "..." : ""
        )
    );

static void PrintElvesDiagram(ISet<Position> elves)
{
    (int minRow, int maxRow, int minCol, int maxCol) = GetBounding(elves);

    for (int row = minRow; row <= maxRow; row++)
    {
        for (int col = minCol; col <= maxCol; col++)
        {
            var position = new Position(row, col);
            char draw = elves.Contains(position) ? '#' : '.';
            Console.Write(draw);
        }
        Console.WriteLine();
    }
}

static int CountEmpty(ISet<Position> elves)
{
    (int minRow, int maxRow, int minCol, int maxCol) = GetBounding(elves);
    int height = maxRow - minRow;
    int width = maxCol - minCol;
    int area = height * width;
    int empty = area - elves.Count();

    return empty;
}

static class Proposer
{
    private static readonly Direction[] directions = new Direction[4] { Direction.N, Direction.S, Direction.W, Direction.E };

    public static Direction? Propose(Position elf, ISet<Position> elves, int offset)
    {
        // "If no other Elves are in one of those eight positions, the Elf does
        // not do anything during this round."
        var neighbours = elf.Neighbours();
        if (neighbours.All(pos => !elves.Contains(pos)))
        {
            return null;
        }

        // loop through the candidate directions in preference order
        // return the first direction that we are not blocked to move in
        for (int i = 0; i < directions.Length; i++)
        {
            var direction = directions[(offset + i) % directions.Length];

            var (blockerdir1, blockerdir2, blockerdir3) = BlockerDirections(direction);

            bool blocked = IsBlocked(blockerdir1) || IsBlocked(blockerdir2) || IsBlocked(blockerdir3);
            if (!blocked)
            {
                return direction;
            }
        }

        return null;

        bool IsBlocked(Direction blockerdir) =>
            elf.Step(blockerdir) is var blockerpos && elves.Contains(blockerpos);

        static (Direction, Direction, Direction) BlockerDirections(Direction d) => d switch
        {
            // "If there is no Elf in the N, NE, or NW adjacent positions, the Elf
            // proposes moving north one step."
            Direction.N => (Direction.N, Direction.NE, Direction.NW),

            // "If there is no Elf in the S, SE, or SW adjacent positions, the Elf
            // proposes moving south one step."
            Direction.S => (Direction.S, Direction.SE, Direction.SW),

            // "If there is no Elf in the W, NW, or SW adjacent positions, the Elf
            // proposes moving west one step."
            Direction.W => (Direction.W, Direction.NW, Direction.SW),

            // "If there is no Elf in the E, NE, or SE adjacent positions, the Elf
            // proposes moving east one step."
            Direction.E => (Direction.E, Direction.NE, Direction.SE),

            _ => throw new ArgumentException($"direction {d} unhandled"),
        };
    }
}

enum Direction { N, NE, E, SE, S, SW, W, NW };

record Position(int Row, int Column)
{
    public override string ToString() => $"({Row},{Column})";

    public Position[] Neighbours() => new Position[] {
        this.Step(Direction.N),
        this.Step(Direction.NE),
        this.Step(Direction.E),
        this.Step(Direction.SE),
        this.Step(Direction.S),
        this.Step(Direction.SW),
        this.Step(Direction.W),
        this.Step(Direction.NW),
    };

    public Position Step(Direction? direction) => direction switch
    {
        Direction.N => this with { Row = this.Row - 1 },
        Direction.NE => this with { Row = this.Row - 1, Column = this.Column + 1 },
        Direction.E => this with { Column = this.Column + 1 },
        Direction.SE => this with { Row = this.Row + 1, Column = this.Column + 1 },
        Direction.S => this with { Row = this.Row + 1 },
        Direction.SW => this with { Row = this.Row + 1, Column = this.Column - 1 },
        Direction.W => this with { Column = this.Column - 1 },
        Direction.NW => this with { Row = this.Row - 1, Column = this.Column - 1 },
        _ => throw new ArgumentException($"{direction} unhandled"),
    };
}
