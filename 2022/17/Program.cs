const int minX = 0;
const int maxX = 6; // "exactly seven units wide" (and we're zero indexed)
const int minY = 0;
const int numRocks = 2022; // "after 2022 rocks have stopped"

string file = args.Length == 1 ? args[0] : "eg.txt";
char[] jetPattern = File.ReadAllText(file).ToCharArray();
var occupied = new HashSet<Point>();

var rocks = GetRocks();

DebugDrawRocks(rocks);

int currentJetIndex = 0;
int currentHeight = 0;
for (int i = 0; i <= numRocks - 1; i++)
{
    Console.WriteLine($"simulating rock {i + 1} (current height: {currentHeight})");

    // "each rock appears so that...
    // ...its left edge is two units away from the left wall and..."
    const int relativeX = +2;

    // "...its bottom edge is three units above the highest rock in the room"
    int relativeY = currentHeight + 3;

    var shape = rocks[i % rocks.Length];
    var position = new Vector(relativeX, relativeY);
    var rock = new Rock(shape, position);

    while (true) // until rock comes to rest
    {
        DebugDraw(rock, currentHeight, occupied);

        // alternates between being pushed by a jet of hot gas one unit...
        {
            char jet = jetPattern[currentJetIndex];
            currentJetIndex = (currentJetIndex + 1) % jetPattern.Length;

            var push = jet switch
            {
                '<' => new Vector(-1, 0),
                '>' => new Vector(+1, 0),
                _ => throw new ArgumentException($"unsupported: {jet}"),
            };

            (_, rock) = rock.TryMove(push, occupied, minX, maxX, minY);

            DebugDraw(rock, currentHeight, occupied);
        }

        // ...and then falling one unit down
        {
            var gravity = new Vector(0, -1);
            (bool clipping, rock) = rock.TryMove(gravity, occupied, minX, maxX, minY);
            if (clipping)
            {
                // "If a downward movement would have caused a falling rock to
                // move into the floor or an already-fallen rock, the falling
                // rock stops where it is (having landed on something)"
                foreach (var point in rock.Points)
                {
                    occupied.Add(point);
                }

                // we're zero indexed: a rock piece at Y=0 has a height of 1
                currentHeight = Math.Max(currentHeight, rock.MaxY() + 1);

                DebugDraw(null, currentHeight, occupied);

                break;
            }
        }
    }
}

Console.WriteLine($"current height is {currentHeight}");

static Shape[] GetRocks() => new[] {
    // Y=0 is the bottom

    // ####
    new Shape(
        new Point[] { new (0,0), new (1,0), new (2,0), new (3,0) }
    ),

    // .#.
    // ###
    // .#.
    new Shape(
        new Point[] { new (1,2), new (0,1), new (1,1), new (2,1), new (1,0) }
    ),

    // ..#
    // ..#
    // ###
    new Shape(
        new Point[] { new (2,2), new (2,1), new (0,0), new (1,0), new (2,0) }
    ),

    // #
    // #
    // #
    // #
    new Shape(
        new Point[] { new(0,3), new(0,2), new(0,1), new(0,0) }
    ),

    // ##
    // ##
    new Shape(
        new Point[] { new(0,1), new(1,1), new(0,0), new(1,0) }
    ),
};

static void DebugDraw(Rock? rock, int currentHeight, ISet<Point> occupied)
{
#if DEBUG
    int maxY = rock != null ? rock.MaxY() : currentHeight;
    for (int y = maxY; y >= minY - 1; y--)
    {
        for (int x = minX - 1; x <= maxX + 1; x++)
        {
            var point = new Point(x, y);

            bool isWall = x == minX - 1 || x == maxX + 1;
            bool isFloor = y == minY - 1;

            char draw;
            if (isWall && isFloor) { draw = '+'; }                         // "corner" between wall and floor
            else if (isWall) { draw = '|'; }                               // wall
            else if (isFloor) { draw = '_'; }                              // floor
            else if (rock != null && rock.Contains(point)) { draw = '@'; } // active rock
            else if (occupied.Contains(point)) { draw = '#'; }             // settled rock
            else { draw = '.'; }                                           // empty

            Console.Write(draw);
        }
        Console.WriteLine();
    }
    Console.WriteLine();
#endif
}

static void DebugDrawRocks(Shape[] rocks)
{
#if DEBUG
    for (int i = 0; i < rocks.Length; i++)
    {
        Console.WriteLine($"rock {i}:");
        var diagram = rocks[i].ToDiagram();
        // Y=0 is the bottom so print it last
        for (int y = diagram.GetLength(0) - 1; y >= 0; y--)
        {
            for (int x = 0; x < diagram.GetLength(1); x++)
            {
                Console.Write(diagram[y, x]);
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
#endif
}

record Rock(Shape Shape, Vector Offset)
{
    public IEnumerable<Point> Points => Shape.Points.Select(p => p.Translate(Offset));

    private bool IsClipping(ISet<Point> occupied, int minX, int maxX, int minY) =>
        Points.Any(p => p.X < minX || p.X > maxX || p.Y < minY || occupied.Contains(p));

    public int MaxY() => Points.Select(p => p.Y).Max();

    public bool Contains(Point point) => Points.Any(p => p == point);

    public (bool, Rock) TryMove(Vector v, ISet<Point> occupied, int minX, int maxX, int minY)
    {
        var moved = this with { Offset = Offset.Add(v) };
        return moved.IsClipping(occupied, minX, maxX, minY)
            ? (true, this)
            : (false, moved);
    }
}

record Shape(IEnumerable<Point> Points)
{
    private int MinX = Points.MinBy(p => p.X)!.X;
    private int MinY = Points.MinBy(p => p.Y)!.Y;
    private int MaxX = Points.MaxBy(p => p.X)!.X;
    private int MaxY = Points.MaxBy(p => p.Y)!.Y;

    public int Width { get => MaxX - MinX + 1; }
    public int Height { get => MaxY - MinY + 1; }

    public char[,] ToDiagram(char filled = '#')
    {
        // initialize diagram
        var diagram = new char[this.Height, this.Width];
        for (int y = 0; y < diagram.GetLength(0); y++)
        {
            for (int x = 0; x < diagram.GetLength(1); x++)
            {
                diagram[y, x] = '.';
            }
        }

        // fill in points
        foreach (var point in Points)
        {
            int x = point.X - MinX;
            int y = point.Y - MinY;
            diagram[y, x] = filled;
        }

        return diagram;
    }
}

record Vector(int X, int Y)
{
    public Vector Add(Vector v) => this with { X = this.X + v.X, Y = this.Y + v.Y };
}

record Point(int X, int Y)
{
    public Point Translate(Vector v) => this with { X = this.X + v.X, Y = this.Y + v.Y };
}
