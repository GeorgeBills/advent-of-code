using System.Text.RegularExpressions;

var stepRegex = new Regex(@"^move (\d+) from (\d+) to (\d+)$");

var parseDiagram = (string[] lines) =>
{
    var stacks = new List<Stack<char>>();

    char[][] diagram = lines.Select(line => line.ToCharArray()).ToArray();

    int height = diagram.Length;
    int width = diagram[0].Length; // rely on all lines being the same length

    /*************
     *    [D]    *
     *[N] [C]    *
     *[Z] [M] [P]*
     * 1   2   3 *
     *************/

    for (int x = 1; x < width; x += 4)
    {
        var crates = new Stack<char>();

        for (int y = height - 2; y >= 0; y--)
        {
            char crate = diagram[y][x];
            if (crate == ' ')
            {
                break;
            }
            // Console.WriteLine(new { x, y, crate });
            crates.Push(crate);
        }

        stacks.Add(crates);
    }

    return stacks.ToArray();
};

var printCrates = (Stack<char>[] crates) =>
{
    for (int i = 0; i < crates.Length; i++)
    {
        Console.WriteLine($"{i + 1}: {string.Join(',', crates[i])}");
    }
};

var parseSteps = (string[] lines) => lines
    .Select(line => stepRegex.Match(line).Groups)
    .Select(groups => (new Step(int.Parse(groups[1].Value), int.Parse(groups[2].Value), int.Parse(groups[3].Value))));

string[] lines = File.ReadAllLines(args[0]);
int splitidx = Array.FindIndex(lines, line => line == ""); // diagram / steps are split by an empty line

var crates = parseDiagram(lines[..splitidx]);

Console.WriteLine("initial crates:");
printCrates(crates);
Console.WriteLine();

var steps = parseSteps(lines[(splitidx + 1)..]);

foreach (var step in steps)
{
    Console.WriteLine($"move {step.move} from {step.from} to {step.to}");
    int stackidx = step.from - 1;
    int toidx = step.to - 1;
    for (int i = 0; i < step.move; i++)
    {
        char crate = crates[stackidx].Pop();
        crates[toidx].Push(crate);
    }

    printCrates(crates);
    Console.WriteLine();
}

char[] topCrates = crates.Select(stack => stack.Peek()).ToArray();
Console.WriteLine(topCrates);

record Step(int move, int from, int to);
