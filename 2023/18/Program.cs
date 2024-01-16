using System.Drawing;
using System.Text.RegularExpressions;

using RGB = (byte R, byte G, byte B);
using Point = (int X, int Y);
using Vector = (int X, int Y);
using Edge = ((int X, int Y) A, (int X, int Y) B);

const string file = "eg.txt";

var plan = File.ReadLines(file).Select(ParseDigPlanInstruction);
var edges = TrenchEdges(plan).Select(e => e.Edge);

var p = edges.SelectMany(e => new[] { e.A, e.B }).ToHashSet();
for (int i = 5; i > -15; i--)
{
    for (int j = -5; j < 15; j++)
    {
        Console.Write(p.Contains((j, i)) ? '#' : '.');
    }
    Console.WriteLine();
}

// shoelace formula: areas of triangles defined by points (0,0), A, B
var areas = edges.Select(e => e.A.X * e.B.Y - e.B.X * e.A.Y);
Console.WriteLine(string.Join(',', Enumerable.Zip(edges, areas)));

// #if WINDOWS
var points = edges.SelectMany(e => new[] { e.A, e.B });
var bb = BoundingBox(points);

var translate = ScaleVector(bb.Min, -1);
var scale = 100;

var edgestransformed = edges.Select(e => TranslateEdge(e, translate)).Select(e => ScaleEdge(e, scale)).ToArray();

var bbtransformed = (Min: TranslatePoint(bb.Min, translate), Max: TranslatePoint(bb.Max, translate));
var drawpoints = edgestransformed.Select(e => new System.Drawing.Point[] { new(0, 0), new(e.A.X, e.A.Y), new(e.B.X, e.B.Y) });

var cols = new[] {
    Pens.Red,
    Pens.Blue,
    Pens.Yellow,
    Pens.Green,
    Pens.Orange,
    Pens.Purple,
    Pens.Pink,
    Pens.Brown,
    Pens.Black,
    Pens.White,
    Pens.Gray,
    Pens.Beige,
    Pens.Turquoise,
    Pens.Cyan,
};

int pen = 0;

// TODO: use SkiaSharp!
// TODO: draw the triangles as transparent polys
using (var bitmap = new Bitmap(bbtransformed.Max.X * scale + 1, bbtransformed.Max.Y * scale + 1))
using (var graphics = Graphics.FromImage(bitmap))
{
    graphics.Clear(Color.Black);
    foreach (var dp in drawpoints)
    {
        graphics.DrawLine(cols[pen++], dp[1], dp[2]); // TODO: use the defined colour!
        // graphics.FillPolygon(Brushes.Blue, dp, );
        // graphics.DrawPolygon(cols[pen++ % cols.Length], dp);
    }
    bitmap.Save("tmp.png");
}
// #endif

double area = Math.Abs(areas.Sum()) / 2.0;
Console.WriteLine(area);

static (Point Min, Point Max) BoundingBox(IEnumerable<Point> points) => points.Aggregate(
    (Min: (X: int.MaxValue, Y: int.MaxValue), Max: (X: int.MinValue, Y: int.MinValue)),
    (bb, p) => (
        (Math.Min(bb.Min.X, p.X), Math.Min(bb.Min.Y, p.Y)),
        (Math.Max(bb.Max.X, p.X), Math.Max(bb.Max.Y, p.Y))
    )
);

static Edge ScaleEdge(Edge e, int scale) => (ScalePoint(e.A, scale), ScalePoint(e.B, scale));

static Point ScalePoint(Point p, int scale) => (p.X * scale, p.Y * scale);

static Vector ScaleVector(Vector v, int scale) => (v.X * scale, v.Y * scale);

static Point TranslatePoint(Point p, Vector v) => (p.X + v.X, p.Y + v.Y);

static Edge TranslateEdge(Edge e, Vector v) => (TranslatePoint(e.A, v), TranslatePoint(e.B, v));

static bool IsRectangleEdges(Edge a, Edge b) => IsRectanglePoints(a.A, a.B, b.A, b.B);

static bool IsRectanglePoints(Point a, Point b, Point c, Point d)
{
    // sort so that a and b have the minimum y values (ab and cd are the horizontal edges)
    if (a.Y > c.Y) { (a, c) = (c, a); }
    if (b.Y > d.Y) { (b, d) = (d, b); }

    // sort so that a and c have the minimum x values (ac and bd are the vertical edges)
    if (a.X > b.X) { (a, b) = (b, a); }
    if (c.X > d.X) { (c, d) = (d, c); }

    return a.Y == b.Y && c.Y == d.Y && a.X == c.X && b.X == d.X;
}

static Orientation CategoriseOrientation(Edge e) => (e.A.X, e.A.Y, e.B.X, e.B.Y) switch
{
    (var ax, _, var bx, _) when ax == bx => Orientation.Vertical,
    (_, var ay, _, var by) when ay == by => Orientation.Horizontal,
    _ => Orientation.Diagonal,
};

static (Direction Direction, int Distance, (byte R, byte G, byte B) RGB) ParseDigPlanInstruction(string instruction)
{
    const string pattern = @"^(?<direction>[LRUD]) (?<distance>\d+) \(#(?<colour>[0-9a-f]{6})\)$";
    var match = Regex.Match(instruction, pattern);
    if (!match.Success)
    {
        throw new ArgumentException($"couldn't parse {instruction}");
    }

    var direction = match.Groups["direction"].ValueSpan switch
    {
        "L" => Direction.L,
        "R" => Direction.R,
        "U" => Direction.U,
        "D" => Direction.D
    };
    int distance = int.Parse(match.Groups["distance"].ValueSpan);
    byte[] rgb = Convert.FromHexString(match.Groups["colour"].ValueSpan);

    return (direction, distance, (rgb[0], rgb[1], rgb[2]));
}

static IEnumerable<(Edge Edge, RGB Colour)> TrenchEdges(IEnumerable<(Direction, int, RGB)> plan)
{
    var a = (X: 0, Y: 0); // arbitrary starting point
    foreach (var instruction in plan)
    {
        var (direction, distance, colour) = instruction;
        (int X, int Y) b = direction switch
        {
            Direction.L => (a.X - distance, a.Y),
            Direction.R => (a.X + distance, a.Y),
            Direction.U => (a.X, a.Y + distance),
            Direction.D => (a.X, a.Y - distance),
        };
        var ab = (((a.X, a.Y), (b.X, b.Y)), colour);
        yield return ab;
        a = b; // last point of this edge is the first point for the next edge
    }
}

enum Direction { L, R, U, D }

enum Orientation { Horizontal, Vertical, Diagonal }
