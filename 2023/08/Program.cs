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

uint step = 0;
var currents = nodesdict.Values.Where(n => n.ID.EndsWith(startch)).ToArray();

PrintNodes(step, currents);

while (!currents.All(n => n.ID.EndsWith(endch)))
{
    uint lridx = (uint)(step % lrs.Length);
    var lr = lrs[lridx];

    for (int i = 0; i < currents.Length; i++)
    {
        var current = currents[i];
        var next = lr switch { LR.L => current.L, LR.R => current.R };
        currents[i] = next;
    }

    step++;

    if (step < 100 || step % 1_000_000 == 0)
    {
        PrintNodes(step, currents);
    }
}

PrintNodes(step, currents);

static void PrintNodes(uint step, Node[] currents) =>
    Console.WriteLine($"{step,13:N0}: {string.Join(' ', currents.ToArray<object>())}");

enum LR { L, R };

class Node
{
    public string? ID { get; init; }
    public Node? L { get; set; }
    public Node? R { get; set; }
    public override string ToString() => ID;
}
