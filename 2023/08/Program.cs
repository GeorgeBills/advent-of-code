string file = args[0];

var input = File.ReadAllLines(file);

var lr = input.First().Select(c => c switch { 'L' => LR.L, 'R' => LR.R }).ToArray();

const string start = "AAA";
const string end = "ZZZ";

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
var current = nodesdict[start];
do
{
    Console.WriteLine(current.ID);

    current = lr[step % lr.Length] switch { LR.L => current.L, LR.R => current.R };
    step++;
} while (current!.ID != end);

Console.WriteLine($"reached {current.ID} after {step} steps");

enum LR { L, R };

class Node
{
    public string? ID { get; init; }
    public Node? L { get; set; }
    public Node? R { get; set; }
}
