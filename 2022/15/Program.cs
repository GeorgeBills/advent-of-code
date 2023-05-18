using System.Diagnostics;
using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

var (sensors, beacons, searched) = Parse(file);

var box = new Rectangle(
    MinX: 0,
    MinY: 0,
    MaxX: 20,
    MaxY: 20
);

#if DEBUG
Console.WriteLine($"bounding box: {box}");
#endif

#if DEBUG
if (box.Height < 100 && box.Width < 100)
{
    PrintDiagram(box, sensors, beacons, searched);
}
else
{
    // full input is ~six million coordinates wide by ~six million coordinates high
    Console.WriteLine($"not drawing diagram given size (H{box.Height} x W{box.Width})");
}
#endif

var stopwatch = new Stopwatch();
stopwatch.Start();
var point = Search(box);
stopwatch.Stop();

if (point == null)
{
    throw new Exception("no point found?!?");
}

int frequency = point.Value.X * 4_000_000 + point.Value.Y;
Console.WriteLine($"point is at {point} with frequency {frequency} (took {stopwatch.Elapsed})");

const int maxSearchDepth = 10;

Point? Search(Rectangle rect, int depth = 0)
{
    if (depth > maxSearchDepth)
    {
        return null;
    }

    var circle = searched.FirstOrDefault(circle => circle.Contains(rect));
    if (circle != default)
    {
#if DEBUG
        Console.WriteLine($"{circle} fully contains {rect}");
#endif
        return null;
    }

#if DEBUG
    Console.WriteLine($"searching {rect}");
#endif

    if (rect.Area == 1)
    {
        var point = new Point(rect.MinX, rect.MinY);
        var circ = searched.FirstOrDefault(circle => circle.Contains(point));
        if (circ != default)
        {
            // we're conservative (rounding radius down) in our "circle contains
            // rectangle" checks so need to check if any of the searched circles
            // contain this point before we can return it.
            Console.WriteLine($"{circle} contains {point}");
            return null;
        }

        return point; // found it!
    }

    var (nw, ne, sw, se) = Split(rect);

    var rects = new[] { nw, ne, sw, se };
    return rects
        .Select(r => Search(r, depth + 1))
        .FirstOrDefault(point => point != null);
}

(Rectangle NW, Rectangle NE, Rectangle SW, Rectangle SE) Split(Rectangle rect)
{
    int halfWidth = rect.Width / 2;
    int halfHeight = rect.Height / 2;

    var nw = new Rectangle(
        rect.MinX,
        rect.MinY,
        rect.MinX + halfWidth,
        rect.MinY + halfHeight
    );
    var ne = new Rectangle(
        rect.MinX + halfWidth + 1,
        rect.MinY,
        rect.MaxX,
        rect.MinY + halfHeight
    );
    var sw = new Rectangle(
        rect.MinX,
        rect.MinY + halfHeight + 1,
        rect.MinX + halfWidth,
        rect.MaxY
    );
    var se = new Rectangle(
        rect.MinX + halfWidth + 1,
        rect.MinY + halfHeight + 1,
        rect.MaxX,
        rect.MaxY
    );

    return (nw, ne, sw, se);
}

bool MayContainBeacon(Point point, ISet<Point> beacons, IEnumerable<Circle> searched) =>
    beacons.Contains(point) || !searched.Any(circle => circle.Contains(point));

void PrintDiagram(Rectangle box, ISet<Point> sensors, ISet<Point> beacons, IEnumerable<Circle> searched)
{
    for (int y = box.MinY; y < box.MaxY + 1; y++)
    {
        for (int x = box.MinX; x < box.MaxX + 1; x++)
        {
            var point = new Point(x, y);
            char draw;
            if (sensors.Contains(point)) { draw = 'S'; }
            else if (beacons.Contains(point)) { draw = 'B'; }
            else if (!MayContainBeacon(point, beacons, searched)) { draw = 'x'; }
            else { draw = '.'; }
            Console.Write(draw);
        }
        Console.WriteLine();
    }
}

(ISet<Point> sensors, ISet<Point> beacons, IEnumerable<Circle> searched) Parse(string file)
{
    var sensorRegex = new Regex(@"^Sensor at x=(-?\d+), y=(-?\d+): closest beacon is at x=(-?\d+), y=(-?\d+)$");

    var parsed = File.ReadLines(file)
        .Select(line => sensorRegex.Match(line).Groups)
        .Select(groups =>
            new
            {
                sensor = new Point(X: int.Parse(groups[1].Value), Y: int.Parse(groups[2].Value)),
                beacon = new Point(X: int.Parse(groups[3].Value), Y: int.Parse(groups[4].Value)),
            }
        )
        .Select(sb =>
            new
            {
                sb.sensor,
                sb.beacon,
                distance = sb.sensor.ManhattanDistance(sb.beacon),
            }
        );

#if DEBUG
    Console.WriteLine(String.Join(Environment.NewLine, parsed));
#endif

    var sensors = parsed.Select(sbd => sbd.sensor).ToHashSet();
    var beacons = parsed.Select(sbd => sbd.beacon).ToHashSet();
    var searched = parsed.Select(sbd => new Circle(centre: sbd.sensor, radius: sbd.distance));

    return (sensors, beacons, searched);
}

readonly record struct Circle(Point centre, int radius)
{
    private Point NW { get => centre with { X = centre.X - radius / 2, Y = centre.Y + radius / 2 }; }
    private Point NE { get => centre with { X = centre.X + radius / 2, Y = centre.Y + radius / 2 }; }
    private Point SW { get => centre with { X = centre.X - radius / 2, Y = centre.Y - radius / 2 }; }
    private Point SE { get => centre with { X = centre.X + radius / 2, Y = centre.Y - radius / 2 }; }

    public bool Contains(Point p) => centre.ManhattanDistance(p) <= radius;
    public bool Contains(Rectangle r) =>
        this.NW.X <= r.MinX && this.NW.Y >= r.MaxY &&
        this.NE.X >= r.MaxX && this.NE.Y >= r.MaxY &&
        this.SW.X <= r.MinX && this.SW.Y <= r.MinY &&
        this.SE.X >= r.MaxX && this.SE.Y <= r.MinY;
}

readonly record struct Point(int X, int Y)
{
    public int ManhattanDistance(Point to) => Math.Abs(this.X - to.X) + Math.Abs(this.Y - to.Y);
    public override string ToString() => $"{X},{Y}";
}

readonly record struct Rectangle(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Height { get => MaxY - MinY + 1; }
    public int Width { get => MaxX - MinX + 1; }
    public int Area { get => Height * Width; }
}
