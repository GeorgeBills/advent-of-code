string file = args[0];

var input = File.ReadAllLines(file);

var lrs = input.First().Select(c => c switch { 'L' => LR.L, 'R' => LR.R }).ToArray();

const char startch = 'A';
const char endch = 'Z';

var nodesarr = input
    .Skip(2)
    .Select(str => (ID: str[0..3], L: str[7..10], R: str[12..15]))
    .ToArray();

var nodesdict = nodesarr.ToDictionary(node => node.ID, node => new Node { ID = node.ID });

foreach (var node in nodesarr)
{
    var (id, l, r) = node;
    nodesdict[id].L = nodesdict[l];
    nodesdict[id].R = nodesdict[r];
}

var starts = nodesdict.Values.Where(n => n.ID.EndsWith(startch)).ToArray();
var cycles = new Dictionary<Node, int>();

foreach (var start in starts)
{
    int step = 0;
    var current = start;
    var seen = new Dictionary<(Node, int lridx), int>();

    while (true)
    {
        int lridx = (int)(step % lrs.Length);
        var lr = lrs[lridx];
        current = lr switch { LR.L => current.L, LR.R => current.R };

        step++;

        if (current.ID.EndsWith(endch))
        {
            Console.WriteLine($"{start} (step {step}): finds {current} after {step} total steps");
        }

        var key = (current, lridx);

        if (seen.TryGetValue(key, out int when))
        {
            Console.WriteLine($"{start} (step {step}): last saw {key} at step {when} (cycle length: {step - when} steps)");
            cycles[start] = step - when; // happily the same thing as the depth to z
            break;
        }

        seen.TryAdd(key, step);
    }
}

// every path will both (a) find a z node after n steps and (b) cycle every n steps
// e.g. from the example path 1 finds its z node every 2 steps and path 2 finds its z node every 3 steps
// so all paths will simultaneously find their z node after the least common multiple of their cycle length
// e.g. from the example the least common multiple of 2 and 3 is 6
// this wouldn't be true if the cycle length wasn't the same as the distance to z 
ulong lcm = cycles.Values.Aggregate(1UL, (a, b) => LeastCommonMultiple((ulong)a, (ulong)b));
Console.WriteLine($"all positions will be at z nodes after {lcm} steps");

ulong GreatestCommonDivisor(ulong a, ulong b) // https://en.wikipedia.org/wiki/Euclidean_algorithm
{
    ulong tmp;
    while (b != 0)
    {
        tmp = b;
        b = a % b;
        a = tmp;
    }
    return a;
}

ulong LeastCommonMultiple(ulong a, ulong b) => a * b / GreatestCommonDivisor(a, b);

enum LR { L, R };

class Node
{
    public string? ID { get; init; }
    public Node? L { get; set; }
    public Node? R { get; set; }
    public override string ToString() => ID;
}
