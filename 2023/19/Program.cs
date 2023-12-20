using System.Text.RegularExpressions;

const string file = "eg.txt";

const string start = "in"; // "All parts begin in the workflow named in."
const string destaccepted = "A";
const string destrejected = "R";

var lines = File.ReadAllLines(file);

var workflows = lines.TakeWhile(l => l != "").Select(ParseWorkflow).ToDictionary(wf => wf.ID, wf => wf);
var parts = lines.SkipWhile(l => l != "").Skip(1).Select(ParsePart).ToArray();

#if DEBUG
const int max = 15;
Console.WriteLine(string.Join(Environment.NewLine, workflows.Values.Take(max)));
Console.WriteLine(string.Join(Environment.NewLine, parts.Take(max)));
#endif

// part one
{
    var (accepted, rejected) = Assess1(workflows, parts);
    Console.WriteLine($"{accepted.Count()} accepted (total ratings: {accepted.Sum(p => p.SumRatings())}); {rejected.Count()} rejected");
}

// part two
{
    var ranges = FindAcceptedRanges(workflows);

#if DEBUG
    Console.WriteLine("accepting:");
    foreach (var r in ranges)
    {
        Console.WriteLine(r);
    }
#endif

    // var a = ranges.Aggregate(
    //     new PartRange(X: new(), M: new(), A: new(), S: new()),
    //     (acc, pr) => acc with
    //     {
    //         X = acc.X with { Min = Math.Max(acc.X.Min, pr.X.Min), Max = Math.Max(acc.X.Max, pr.X.Max) },
    //         M = acc.M with { Min = Math.Max(acc.M.Min, pr.M.Min), Max = Math.Max(acc.M.Max, pr.M.Max) },
    //         A = acc.A with { Min = Math.Max(acc.A.Min, pr.A.Min), Max = Math.Max(acc.A.Max, pr.A.Max) },
    //         S = acc.S with { Min = Math.Max(acc.S.Min, pr.S.Min), Max = Math.Max(acc.S.Max, pr.S.Max) },
    //     }
    // );
    // Console.WriteLine($"{a} {a.Combinations()}");

    Console.WriteLine($"there are {ranges.Sum(r => r.Combinations())} total accepted combinations");
}

static IEnumerable<PartRange> FindAcceptedRanges(IDictionary<string, Workflow> workflows)
{
    var initpr = new PartRange(X: new(), M: new(), A: new(), S: new());

    var ranges = Assess(workflows, (initpr, start));
    foreach (var r in ranges)
    {
        yield return r;
    }

    static IEnumerable<PartRange> Assess(IDictionary<string, Workflow> workflows, params (PartRange Range, string Destination)[] nexts)
    {
        foreach (var (range, dest) in nexts)
        {
            if (dest == destaccepted)
            {
                yield return range;
                continue;
            }

            if (dest == destrejected)
            {
                continue;
            }

            var wf = workflows[dest];
            var nextnexts = wf.Next(range).ToArray();

            var ranges = Assess(workflows, nextnexts);
            foreach (var r in ranges)
            {
                yield return r;
            }
        }
    }
}

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

static (IEnumerable<Part> Accepted, IEnumerable<Part> Rejected) Assess1(IDictionary<string, Workflow> workflows, Part[] parts)
{
    var accepted = new List<Part>();
    var rejected = new List<Part>();

    foreach (var part in parts)
    {

#if DEBUG
        Console.Write($"{part}: ");
#endif

        string next = start; // "All parts begin in the workflow named in."
        while (next != destaccepted && next != destrejected)
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

    public ((PartRange Range, string Destination)? Accept, PartRange Reject) Filter(PartRange pr) => Operation switch
    {
        // if the range could be less than the number
        // accept the subrange that is less than that number
        // reject the remainder of the range
        Operation.LT when pr.Range(Category) is var cat && cat.Min < Number =>
            (Accept: (pr.ClampMax(Category, Number), Destination),
             Reject: pr.ClampMin(Category, Number)),

        // if the range could be greater than the number
        // accept the subrange that is greater than that number
        // reject the remainder of the range
        Operation.GT when pr.Range(Category) is var cat && cat.Max > Number =>
            (Accept: (pr.ClampMin(Category, Number), Destination),
             Reject: pr.ClampMax(Category, Number)),

        _ => (null, pr /* unchanged */),
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

    public IEnumerable<(PartRange Range, string Destination)> Next(PartRange pr)
    {
        foreach (var rule in Rules)
        {
            var (accepted, rejected) = rule.Filter(pr);
            if (accepted.HasValue)
            {
                // some subrange of the range was accepted by the rule and can flow to the destination
                yield return accepted.Value;
            }
            pr = rejected; // TODO: optimisation: can stop looping here if the range has zero size
        }
        yield return (pr, Final);
    }
}

record Part(int X, int M, int A, int S)
{
    public override string ToString() => $"{{x={X}, m={M}, a={A}, s={S}}}";

    public int Value(Category category) => category switch
    {
        Category.X => X,
        Category.M => M,
        Category.A => A,
        Category.S => S,
    };

    public int SumRatings() => X + M + A + S;
}

record MinMax(int Min = 1, int Max = 4000)
{
    public override string ToString() => $"{Min,4}..{Max,4}";

    public long Spread() => Max - Min + 1;
}

record PartRange(MinMax X, MinMax M, MinMax A, MinMax S)
{
    public override string ToString() => $"{{x={X} ({X.Spread(),4}), m={M} ({M.Spread(),4}), a={A} ({A.Spread(),4}), s={S} ({S.Spread(),4})}}";

    public MinMax Range(Category category) => category switch
    {
        Category.X => X,
        Category.M => M,
        Category.A => A,
        Category.S => S,
    };

    public PartRange ClampMax(Category category, int number) => category switch
    {
        Category.X => this with { X = X with { Max = Math.Min(X.Max, number - 1) } },
        Category.M => this with { M = M with { Max = Math.Min(M.Max, number - 1) } },
        Category.A => this with { A = A with { Max = Math.Min(A.Max, number - 1) } },
        Category.S => this with { S = S with { Max = Math.Min(S.Max, number - 1) } },
    };

    public PartRange ClampMin(Category category, int number) => category switch
    {
        Category.X => this with { X = X with { Min = Math.Max(X.Min, number + 1) } },
        Category.M => this with { M = M with { Min = Math.Max(M.Min, number + 1) } },
        Category.A => this with { A = A with { Min = Math.Max(A.Min, number + 1) } },
        Category.S => this with { S = S with { Min = Math.Max(S.Min, number + 1) } },
    };

    public long Combinations() => X.Spread() * M.Spread() * A.Spread() * S.Spread();
}
