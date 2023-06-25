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

/*
 *         1111
 *         1111
 *         1111
 *         1111
 * 222233334444
 * 222233334444
 * 222233334444
 * 222233334444
 *         55556666
 *         55556666
 *         55556666
 *         55556666
 */

int edgeLength = map.GetLength(1) / 4; // the map is 4 cube faces wide

// define all the corners for each cube face
// 1UL is the up left corner of face 1, 6DL is the down left corner of face 6, etc
var corner1UL = new Position(Row: 0 * edgeLength + 1, Column: 2 * edgeLength + 1);
var corner1UR = new Position(Row: 0 * edgeLength + 1, Column: 3 * edgeLength);
var corner2UL = new Position(Row: 1 * edgeLength + 1, Column: 0 * edgeLength + 1);
var corner3UL = new Position(Row: 1 * edgeLength + 1, Column: 1 * edgeLength + 1);
var corner4UR = new Position(Row: 1 * edgeLength + 1, Column: 3 * edgeLength);
var corner2DL = new Position(Row: 2 * edgeLength, Column: 0 * edgeLength + 1);
var corner3DL = new Position(Row: 2 * edgeLength, Column: 1 * edgeLength + 1);
var corner5UL = new Position(Row: 2 * edgeLength + 1, Column: 2 * edgeLength + 1);
var corner6UL = new Position(Row: 2 * edgeLength + 1, Column: 3 * edgeLength + 1);
var corner6UR = new Position(Row: 2 * edgeLength + 1, Column: 4 * edgeLength);
var corner5DL = new Position(Row: 3 * edgeLength, Column: 2 * edgeLength + 1);
var corner6DL = new Position(Row: 3 * edgeLength, Column: 3 * edgeLength + 1);

// define all edges for each cube face
// each edge is a sequence of positions along that edge
var edge1U = NewHorizontalEdge(corner1UL, edgeLength);
var edge1L = NewVerticalEdge(corner1UL, edgeLength);
var edge1R = NewVerticalEdge(corner1UR, edgeLength);
var edge2U = NewHorizontalEdge(corner2UL, edgeLength);
var edge2L = NewVerticalEdge(corner2UL, edgeLength);
var edge2D = NewHorizontalEdge(corner2DL, edgeLength);
var edge3U = NewHorizontalEdge(corner3UL, edgeLength);
var edge3D = NewHorizontalEdge(corner3DL, edgeLength);
var edge4R = NewVerticalEdge(corner4UR, edgeLength);
var edge5L = NewVerticalEdge(corner5UL, edgeLength);
var edge5D = NewHorizontalEdge(corner5DL, edgeLength);
var edge6U = NewHorizontalEdge(corner6UL, edgeLength);
var edge6R = NewVerticalEdge(corner6UR, edgeLength);
var edge6D = NewHorizontalEdge(corner6DL, edgeLength);

#if DEBUG
PrintMap(
    map,
    current: null,
    edge1U, edge1L, edge1R, edge2U, edge2L, edge2D, edge3U, edge3D, edge4R, edge5L, edge5D, edge6U, edge6R, edge6D
);
#endif

// define edges that join to other edges
// e.g. join12 is the edge on face 1 joining to the edge on face 2
// the facings are for the facing we'll have after crossing the edge
// (respective to the face) 
var join12 = new Join(
    Enumerable.Zip(edge1U, edge2U.Reverse()),
    Facing.Down, Facing.Down
);
var join13 = new Join(
    Enumerable.Zip(edge1L, edge3U),
    Facing.Right, Facing.Down
);
var join16 = new Join(
    Enumerable.Zip(edge1R, edge6R.Reverse()),
    Facing.Left, Facing.Left
);
var join25 = new Join(
    Enumerable.Zip(edge2D, edge5D.Reverse()),
    Facing.Up, Facing.Up
);
var join26 = new Join(
    Enumerable.Zip(edge2L, edge6D.Reverse()),
    Facing.Right, Facing.Up
);
var join35 = new Join(
    Enumerable.Zip(edge3D, edge5L.Reverse()),
    Facing.Up, Facing.Right
);
var join46 = new Join(
    Enumerable.Zip(edge4R, edge6U.Reverse()),
    Facing.Left, Facing.Down
);

var connections =
    new[] { join12, join13, join16, join25, join26, join35, join46 }
    .SelectMany(
        join =>
            join.PositionPairs.SelectMany(
                pospair => new (PositionFacing From, PositionFacing To)[] {
                    (
                        // edge 1 => edge 2
                        From: new PositionFacing (pospair.PositionEdge1, ReverseFacing(join.FacingEdge1)),
                        To: new PositionFacing (pospair.PositionEdge2, join.FacingEdge2)
                    ),
                    (
                        // edge 2 => edge 1
                        From: new PositionFacing (pospair.PositionEdge2, ReverseFacing(join.FacingEdge2)),
                        To: new PositionFacing (pospair.PositionEdge1, join.FacingEdge1)
                    ),
                    // the direction we're facing when we leave the edge is the
                    // reverse of the direction we should have when we enter the
                    // edge.
                }
            )
    )
    .ToDictionary(fromto => fromto.From, fromto => fromto.To);

#if DEBUG
Console.WriteLine($"{connections.Count()} connections between cube faces");
#endif

foreach (var p in path)
{
    Console.Write($"{p} => "); // path instruction we're executing
    position = p switch
    {
        int steps => position.WithStep(map, connections, steps),
        Turn turn => position.WithTurn(turn),
    };
    Console.WriteLine(position); // resulting position

#if DEBUG
    PrintMap(map, position);
#endif
}

(int row, int column, var facing) = position;

// "The final password is the sum of 1000 times the row, 4 times the column, and
// the facing."
int password =
    1000 * row +
    4 * column +
    // "Facing is 0 for right (>), 1 for down (v), 2 for left (<), and 3 for up (^)."
    facing switch { Facing.Right => 0, Facing.Down => 1, Facing.Left => 2, Facing.Up => 3 };

Console.WriteLine($"the final position is {position}; password: {password}");










static Tile[,] ParseMap(List<string> mapLines)
{
    int width = mapLines.Select(line => line.Length).Max();
    int height = mapLines.Count();

    /* 
     * init the map with a surrounding 1 tile sentinel gap.
     * this costs ((4 + 3) * edgeLength) bytes of memory (negligible).
     * in return it makes our "have left the face" calculations significantly simpler
     * (we can always just check to see if we're on a blank tile).
     * as a nice side effect it aligns us with the 1-indexed rows and columns from the description.
     */
    var map = new Tile[height + 2, width + 2];

    for (int row = 0; row < map.GetLength(0); row++)
    {
        for (int col = 0; col < map.GetLength(1); col++)
        {
            int i = row - 1, j = col - 1; // indexes into mapLines
            bool valididx = i >= 0 && i < mapLines.Count() && j >= 0 && j < mapLines[i].Length;
            map[row, col] = valididx
                ? mapLines[i][j] switch
                {
                    ' ' => Tile.Blank,
                    '#' => Tile.Wall,
                    '.' => Tile.Open,
                }
                : Tile.Blank;
        }
    }

    return map;
}

static void PrintMap(Tile[,] map, PositionFacing? current = null, params IEnumerable<Position>[] edges)
{
    var edgePositions = edges.SelectMany(p => p).ToHashSet();

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
            char draw;
            if (current != null && current.Row == row && current.Column == col)
            {
                draw = (char)current.Facing;
            }
            else if (new Position(row, col) is var pos && edgePositions.Contains(pos))
            {
                draw = 'E';
            }
            else
            {
                draw = (char)map[row, col];
            }

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

static PositionFacing GetStartingPosition(Tile[,] map)
{
    const int y = 1;
    for (int x = 1; x < map.GetLength(1) - 1; x++)
    {
        // "You begin the path in the leftmost open tile of the top row of
        // tiles. Initially, you are facing to the right(from the perspective of
        // how the map is drawn)."
        if (map[y, x] == Tile.Open)
        {
            return new PositionFacing(y, x, Facing.Right);
        }
    }
    throw new Exception("couldn't find starting position");
}

static IEnumerable<Position> NewHorizontalEdge(Position start, int edgeLength) =>
    Enumerable.Range(start.Column, edgeLength).Select(col => new Position(Row: start.Row, col));

static IEnumerable<Position> NewVerticalEdge(Position start, int edgeLength) =>
    Enumerable.Range(start.Row, edgeLength).Select(row => new Position(row, Column: start.Column));

static Facing ReverseFacing(Facing facing) => facing switch
{
    Facing.Up => Facing.Down,
    Facing.Down => Facing.Up,
    Facing.Left => Facing.Right,
    Facing.Right => Facing.Left,
};

enum Tile { Blank = ' ', Open = '.', Wall = '#' };

enum Turn { Right = 'R', Left = 'L' };

enum Facing { Up = '^', Down = 'v', Right = '>', Left = '<' };

record Join(
    IEnumerable<(Position PositionEdge1, Position PositionEdge2)> PositionPairs,
    Facing FacingEdge1,
    Facing FacingEdge2
);

record Position(int Row, int Column);

record PositionFacing(int Row, int Column, Facing Facing) : Position(Row, Column)
{
    public PositionFacing(Position position, Facing facing) : this(position.Row, position.Column, facing) { }
    public PositionFacing WithStep(Tile[,] map, IDictionary<PositionFacing, PositionFacing> connections, int steps)
    {
        var position = this;
        while (steps > 0)
        {
            var prospective = step(position, position.Facing);

            var tile = map[prospective.Row, prospective.Column];
            if (tile == Tile.Blank)
            {
                // we've stepped off the map. we should have instead warped from
                // our current position to the position associated in our
                // connections dict.
                prospective = connections[position];
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

        PositionFacing step(PositionFacing pos, Facing facing) => facing switch
        {
            Facing.Up => pos with { Row = pos.Row - 1 },
            Facing.Down => pos with { Row = pos.Row + 1 },
            Facing.Right => pos with { Column = pos.Column + 1 },
            Facing.Left => pos with { Column = pos.Column - 1 },
        };
    }

    public PositionFacing WithTurn(Turn turn) => this with { Facing = newFacing(turn) };

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
