string file = args.Length == 1 ? args[0] : "eg.txt";

var cubes = File.ReadLines(file)
    .Select(line => line.Split(','))
    .Select(numstrs => numstrs.Select(str => int.Parse(str)))
    .Select(numlist => numlist.ToArray())
    .Select(numarr => (numarr[0], numarr[1], numarr[2]))
    .ToHashSet();

int area = cubes.Select(SurfaceArea).Sum();

IEnumerable<(int x, int y, int z)> OrthogonalNeighbours((int x, int y, int z) cube)
{
    (int x, int y, int z) = cube;
    return new (int, int, int)[6] {
        (x + 1, y, z),
        (x - 1, y, z),
        (x, y + 1, z),
        (x, y - 1, z),
        (x, y, z + 1),
        (x, y, z - 1),
    };
}

int SurfaceArea((int x, int y, int z) cube) => OrthogonalNeighbours(cube).Count(nb => !cubes.Contains(nb));

Console.WriteLine($"total surface area: {area}");
