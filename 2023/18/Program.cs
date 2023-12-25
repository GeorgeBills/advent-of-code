using System.Text.RegularExpressions;

using RGB = (byte R, byte G, byte B);
using Point = (int X, int Y);
using Vector = (int X, int Y);
using Edge = ((int X, int Y) A, (int X, int Y) B);

const string file = "eg.txt";

var plan = File.ReadLines(file).Select(ParseDigPlanInstruction);
Console.WriteLine($"{plan.Count()} instructions");

var points = TrenchPoints(plan).ToArray();

var min = points.Aggregate((X: int.MaxValue, Y: int.MaxValue), (acc, v) => (Math.Min(acc.X, v.X), Math.Min(acc.Y, v.Y)));
var translate = Scale(min, -1);

var pointst = points.Select(p => TranslatePoint(p, translate)).ToArray(); // translate so all points are >= (0,0)
Console.WriteLine($"{pointst.Length} points: {string.Join(',', pointst.Take(15))}{(pointst.Length > 15 ? "..." : "")}");

var edges = TrenchEdges(plan).ToArray();
var edgest = edges.Select(e => (Edge: TranslateEdge(e.Edge, translate), e.Colour)).ToArray(); // translate so all points are >= (0,0)
Console.WriteLine($"{edgest.Length} edges: {string.Join(',', edgest.Select(e => e.Edge))}");

var x = edgest.GroupBy(e => CategoriseOrientation(e.Edge), e => e.Edge).ToDictionary(g => g.Key, g => g);
var h = x[Orientation.Horizontal].Order().ToArray();
var v = x[Orientation.Vertical].OrderBy(e => e.A.Y).ThenBy(e => e.B.Y).ThenBy(e => e.A.X).ToArray();

Console.WriteLine(string.Join(',', v));

Console.WriteLine(IsRectangleEdges(v[0], v[1]));
Console.WriteLine(IsRectangleEdges(v[2], v[3]));
Console.WriteLine(IsRectangleEdges(v[4], v[5]));

// https://en.wikipedia.org/wiki/Rectilinear_polygon
// https://en.wikipedia.org/wiki/Polygon_partition#Partitioning_a_rectilinear_polygon_into_rectangles

static Vector Scale(Vector v, int scale) => (v.X * scale, v.Y * scale);

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
    var a = (X: 0, Y: 0);
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
        a = b;
    }
}

static IEnumerable<Point> TrenchPoints(IEnumerable<(Direction, int, RGB)> plan)
{
    var current = (X: 0, Y: 0);
    foreach (var instruction in plan)
    {
        var (direction, distance, _) = instruction;
        current = direction switch
        {
            Direction.L => (current.X - distance, current.Y),
            Direction.R => (current.X + distance, current.Y),
            Direction.U => (current.X, current.Y + distance),
            Direction.D => (current.X, current.Y - distance),
        };
        yield return current;
    }
}

enum Direction { L, R, U, D }

enum Orientation { Horizontal, Vertical, Diagonal }
