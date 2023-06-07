using System.Diagnostics;

const int minX = 0;
const int maxX = 6; // "exactly seven units wide" (and we're zero indexed)
const int minY = 0;
const long numRocks = 1000000000000; // "after 1000000000000 rocks have stopped" (!!!)

string file = args.Length == 1 ? args[0] : "eg.txt";
char[] jets = File.ReadAllText(file).ToCharArray();
var occupied = new HashSet<Point>();

var shapes = GetRocks();

// Dictionary of previously seen states to the height at that state. If we see a
// state where a rock has stopped moving with the same X offset and the same jet
// pattern index as a previously seen rock of the same shape then we're going to
// repeat history from then on, and can fast-forward.
var history = new Dictionary<State, (long height, long i)>();

// DebugDrawRocks(rocks);

int jetidx = 0;
long height = 0;
var sw = Stopwatch.StartNew();
for (long i = 0; i <= numRocks - 1; i++)
{
    Console.WriteLine($"simulating rock {i + 1} out of {numRocks} ({(double)(i + 1) / numRocks:P}; {sw.ElapsedMilliseconds}ms; current height: {height})");

    int shapeidx = (int)(i % shapes.Length);

    // "each rock appears so that...
    // ...its left edge is two units away from the left wall and..."
    const int relativeX = +2;

    // "...its bottom edge is three units above the highest rock in the room"
    long relativeY = height + 3;

    var shape = shapes[shapeidx];
    var position = new Vector(relativeX, relativeY);
    var rock = new Rock(shape, position);

    while (true) // until rock comes to rest
    {
        // DebugDraw(rock, height, occupied);

        // alternates between being pushed by a jet of hot gas one unit...
        {
            char jet = jets[jetidx];
            jetidx = (jetidx + 1) % jets.Length;

            var push = jet switch
            {
                '<' => new Vector(-1, 0),
                '>' => new Vector(+1, 0),
                _ => throw new ArgumentException($"unsupported: {jet}"),
            };

            (_, rock) = rock.TryMove(push, occupied, minX, maxX, minY);

            // DebugDraw(rock, height, occupied);
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
                height = Math.Max(height, rock.MaxY() + 1);

                var state = new State(shapeidx, jetidx, rock.Offset.X);
                if (history.TryGetValue(state, out var previous))
                {
                    Console.WriteLine($"state {state} last seen with height {previous.height} at iteration {previous.i}");

                    long remaining = numRocks - i;
                    Console.WriteLine($"current height: {height}; current iteration: {i}; {remaining} iterations remaining");

                    long cycles = i - previous.i;
                    Console.WriteLine($"we will repeat the same pattern every {cycles} iterations");

                    long increases = height - previous.height;
                    Console.WriteLine($"each time we repeat that pattern we will add {increases} height");

                    long skipcycles = remaining / cycles;
                    long skipiterations = skipcycles * cycles;
                    long add = skipcycles * increases - 1;
                    long remainder = remaining % cycles;
                    Console.WriteLine($"iterating {skipiterations} more times will add {add} height with {remainder} iterations remaining");

                    i += skipiterations;
                    height += add;
                    Console.WriteLine($"fast-forwarding: i = {i}; height = {height}");

                    // make sure any rocks in remainder clip properly
                    Console.WriteLine($"translating {occupied.Count()} occupied points vertically up by {add}");
                    occupied = occupied.Select(p => p with { Y = p.Y + add }).ToHashSet();

                    // clear history to make sure we don't hit any of these in the remainder
                    // we won't detect the cycle again because remainder is less than a full cycle
                    history.Clear();
                }
                else
                {
                    history[state] = (height, i);
                }

                // DebugDraw(null, height, occupied);

                break;
            }
        }
    }
}

Console.WriteLine($"current height is {height}");

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
    long maxY = rock != null ? rock.MaxY() : currentHeight;
    for (long y = maxY; y >= minY - 1; y--)
    {
        for (long x = minX - 1; x <= maxX + 1; x++)
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

record State(int shapeidx, int jetidx, long xoffset);

record Rock(Shape Shape, Vector Offset)
{
    public IEnumerable<Point> Points => Shape.Points.Select(p => p.Translate(Offset));

    private bool IsClipping(ISet<Point> occupied, int minX, int maxX, int minY) =>
        Points.Any(p => p.X < minX || p.X > maxX || p.Y < minY || occupied.Contains(p));

    public long MaxY() => Points.Select(p => p.Y).Max();

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
    private long MinX = Points.MinBy(p => p.X)!.X;
    private long MinY = Points.MinBy(p => p.Y)!.Y;
    private long MaxX = Points.MaxBy(p => p.X)!.X;
    private long MaxY = Points.MaxBy(p => p.Y)!.Y;

    public long Width { get => MaxX - MinX + 1; }
    public long Height { get => MaxY - MinY + 1; }

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
            long x = point.X - MinX;
            long y = point.Y - MinY;
            diagram[y, x] = filled;
        }

        return diagram;
    }
}

record Vector(long X, long Y)
{
    public Vector Add(Vector v) => this with { X = this.X + v.X, Y = this.Y + v.Y };
}

record Point(long X, long Y)
{
    public Point Translate(Vector v) => this with { X = this.X + v.X, Y = this.Y + v.Y };
}
