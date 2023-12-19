using System.Text.RegularExpressions;

const string file = "in.txt";

var lines = File.ReadAllLines(file);

var workflows = lines.TakeWhile(l => l != "").Select(ParseWorkflow).ToDictionary(wf => wf.ID, wf => wf);
var parts = lines.SkipWhile(l => l != "").Skip(1).Select(ParsePart).ToArray();

#if DEBUG
const int max = 15;
Console.WriteLine(string.Join(Environment.NewLine, workflows.Values.Take(max)));
Console.WriteLine(string.Join(Environment.NewLine, parts.Take(max)));
#endif

var (accepted, rejected) = Assess(workflows, parts);

Console.WriteLine($"{accepted.Count()} accepted (total ratings: {accepted.Sum(p => p.SumRatings())}); {rejected.Count()} rejected");

static Workflow ParseWorkflow(string workflow)
{
    const string pattern = """
                           ^
                           (?<id>[a-z]+)
                           {
                                  (?<rule> (?<cat>[xmas]) (?<opr>[<>]) (?<num>\d+) : (?<dst>[a-z]+|A|R) )
                               (, (?<rule> (?<cat>[xmas]) (?<opr>[<>]) (?<num>\d+) : (?<dst>[a-z]+|A|R) ))*
                               ,
                               (?<final>[a-z]+|A|R)
                           }
                           $
                           """;
    var match = Regex.Match(workflow, pattern, RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
    if (!match.Success)
    {
        throw new ArgumentException($"couldn't parse workflow: {workflow}");
    }

    string id = match.Groups["id"].Value;

    var rules =
        Enumerable.Range(0, match.Groups["rule"].Captures.Count)
        .Select(
            i => new Rule(
                Category: ParseCategory(match.Groups["cat"].Captures[i].Value),
                Operation: ParseOperation(match.Groups["opr"].Captures[i].Value),
                Number: int.Parse(match.Groups["num"].Captures[i].Value),
                Destination: match.Groups["dst"].Captures[i].Value
            )
        )
        .ToArray();

    string final = match.Groups["final"].Value;

    return new(ID: id, Rules: rules, Final: final);
}

static Category ParseCategory(string category) => category switch { "x" => Category.X, "m" => Category.M, "a" => Category.A, "s" => Category.S };

static Operation ParseOperation(string operation) => operation switch { "<" => Operation.LT, ">" => Operation.GT };

static Part ParsePart(string part)
{
    const string pattern = @"^{ x=(?<x>\d+), m=(?<m>\d+), a=(?<a>\d+), s=(?<s>\d+) }$";
    var match = Regex.Match(part, pattern, RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
    if (!match.Success)
    {
        throw new ArgumentException($"couldn't parse part: {part}");
    }

    return new(
        X: int.Parse(match.Groups["x"].Value),
        M: int.Parse(match.Groups["m"].Value),
        A: int.Parse(match.Groups["a"].Value),
        S: int.Parse(match.Groups["s"].Value)
    );
}

static (IEnumerable<Part> Accepted, IEnumerable<Part> Rejected) Assess(Dictionary<string, Workflow> workflows, Part[] parts)
{
    const string start = "in";

    var accepted = new List<Part>();
    var rejected = new List<Part>();

    foreach (var part in parts)
    {

#if DEBUG
        Console.Write($"{part}: ");
#endif

        string next = start; // "All parts begin in the workflow named in."
        while (next != "A" && next != "R")
        {
#if DEBUG
            Console.Write($"{next} -> ");
#endif
            next = workflows[next].Match(part);
        }

#if DEBUG
        Console.Write($"{next}");
#endif

        switch (next)
        {
            case "A":
                accepted.Add(part);
                break;
            case "R":
                rejected.Add(part);
                break;
        }

#if DEBUG
        Console.WriteLine();
#endif

    }

    return (accepted, rejected);
}

enum Category { X = 'x', M = 'm', A = 'a', S = 's' }

enum Operation { LT = '<', GT = '>' }

record Rule(Category Category, Operation Operation, int Number, string Destination)
{
    public override string ToString() => $"{(char)Category}{(char)Operation}{Number}:{Destination}";

    public bool IsMatch(Part part) => Operation switch
    {
        Operation.LT => part.Value(Category) < Number,
        Operation.GT => part.Value(Category) > Number,
    };
}

record Workflow(string ID, IEnumerable<Rule> Rules, string Final)
{
    public override string ToString() => $"{ID}{{{string.Join(',', Rules)},{Final}}}";

    public string Match(Part part)
    {
        foreach (var rule in Rules)
        {
            if (rule.IsMatch(part))
            {
                return rule.Destination;
            }
        }
        return Final;
    }
}

record Part(int X, int M, int A, int S)
{
    public override string ToString() => $"{{x={X},m={M},a={A},s={S}}}";

    public int Value(Category category) => category switch
    {
        Category.X => X,
        Category.M => M,
        Category.A => A,
        Category.S => S,
    };

    public int SumRatings() => X + M + A + S;
}
