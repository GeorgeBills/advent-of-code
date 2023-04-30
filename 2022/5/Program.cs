using System.Text.RegularExpressions;

var part = Part.Two;

var stepRegex = new Regex(@"^move (\d+) from (\d+) to (\d+)$");

var parseDiagram = (string[] lines) =>
{
    var stacks = new List<List<char>>();

    char[][] diagram = lines.Select(line => line.ToCharArray()).ToArray();

    int height = diagram.Length;
    int width = diagram.Select(row => row.Length).Distinct().Single();

    /*************
     *    [D]    *
     *[N] [C]    *
     *[Z] [M] [P]*
     * 1   2   3 *
     *************/

    for (int x = 1; x < width; x += 4)
    {
        var crates = new List<char>();

        for (int y = height - 2; y >= 0; y--)
        {
            char crate = diagram[y][x];
            if (crate == ' ')
            {
                break;
            }
            // Console.WriteLine(new { x, y, crate });
            crates.Add(crate);
        }

        stacks.Add(crates);
    }

    return stacks.ToArray();
};

var printCrates = (List<char>[] crates) =>
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

var applyStepCrateMover9000 = (Step step, List<char>[] crates) =>
{
    // stack semantics
    for (int i = 0; i < step.move; i++)
    {
        var fromStack = crates[step.from - 1];
        var toStack = crates[step.to - 1];

        char crate = fromStack.Last();
        fromStack.RemoveAt(fromStack.Count() - 1);
        toStack.Add(crate);
    }
};

var applyStepCrateMover9001 = (Step step, List<char>[] crates) =>
{
    // "range semantics"?
    var fromStack = crates[step.from - 1];
    var toStack = crates[step.to - 1];

    var moveCrates = fromStack.GetRange(fromStack.Count() - step.move, step.move);
    fromStack.RemoveRange(fromStack.Count() - step.move, step.move);
    toStack.AddRange(moveCrates);
};

var applyStep = part switch
{
    Part.One => applyStepCrateMover9000,
    Part.Two => applyStepCrateMover9001,
    _ => throw new NotImplementedException($"{part} not implemeneted"),
};

foreach (var step in steps)
{
    Console.WriteLine($"move {step.move} from {step.from} to {step.to}");

    applyStep(step, crates);

    printCrates(crates);
    Console.WriteLine();
}

char[] topCrates = crates.Select(stack => stack[^1]).ToArray();
Console.WriteLine(topCrates);

record Step(int move, int from, int to);

enum Part { One, Two };
