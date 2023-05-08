using System.Diagnostics;
using System.Text.RegularExpressions;

string file = args[0];
int ysearch = int.Parse(args[1]);

var (sensors, beacons, searched) = Parse(file);

// draw a bounding box around our search area
var box = searched.Aggregate(
    new Rectangle(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue),
    (acc, circle) =>
        acc with
        {
            MinX = Math.Min(acc.MinX, circle.centre.X - circle.radius),
            MinY = Math.Min(acc.MinY, circle.centre.Y - circle.radius),
            MaxX = Math.Max(acc.MaxX, circle.centre.X + circle.radius),
            MaxY = Math.Max(acc.MaxY, circle.centre.Y + circle.radius),
        }
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

var xrange = Enumerable.Range(box.MinX, box.Width + 1);

Console.WriteLine($"searching y={ysearch} ({xrange.Count()} total squares)");

var sw = new Stopwatch();
sw.Start();
int answer = xrange
    .Select(x => new Point(x, ysearch))
    .Where(p => !MayContainBeacon(p, beacons, searched))
    .Count();
sw.Stop();

Console.WriteLine($"{answer} positions may not contain a beacon (took {sw.Elapsed})");

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
    public bool Contains(Point p) => centre.ManhattanDistance(p) <= radius;
}

readonly record struct Point(int X, int Y)
{
    public int ManhattanDistance(Point to) => Math.Abs(this.X - to.X) + Math.Abs(this.Y - to.Y);
    public override string ToString() => $"{X},{Y}";
}

readonly record struct Rectangle(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Height { get => MaxY - MinY; }
    public int Width { get => MaxX - MinX; }
}
