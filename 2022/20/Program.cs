using System.Diagnostics;

const int maxPrint = 20;

string file = args.Length == 1 ? args[0] : "eg.txt";

var input = File.ReadLines(file).Select(int.Parse);

var first = BuildQuadruplyLinkedList(input);
var originalHead = first;
var currentHead = first;

#if DEBUG
Console.WriteLine("initial");
Console.WriteLine(String.Join(", ", NodesCurrentOrder(currentHead).Take(maxPrint)));
#endif

var sw = Stopwatch.StartNew();
foreach (var node in NodesOriginalOrder(originalHead))
{
    int offset = node.Num; // % input.Count();

#if DEBUG
    Console.WriteLine($"processing {node}; offset: {offset}");
#endif

    // remove node from list
    node.CurrentNext.CurrentPrevious = node.CurrentPrevious;
    node.CurrentPrevious.CurrentNext = node.CurrentNext;
    if (node == currentHead)
    {
        currentHead = node.CurrentNext;
    }

    // reinsert node
    Node spliceAfter;
    if (offset > 0)
    {
        spliceAfter = ScanForward(node, offset);
    }
    else
    {
        offset = Math.Abs(offset) + 1; // plus one because we're including ourselves
        spliceAfter = ScanBackward(node, offset);
    }

#if DEBUG
    Console.WriteLine($"splicing in at {spliceAfter.CurrentPrevious} => {spliceAfter} => {node}* => {spliceAfter.CurrentNext}");
#endif

    // before: splice => splice.next
    // after:  splice => node => splice.next
    node.CurrentNext = spliceAfter.CurrentNext;
    node.CurrentPrevious = spliceAfter;
    if (spliceAfter.CurrentNext == currentHead)
    {
        currentHead = node;
    }
    spliceAfter.CurrentNext.CurrentPrevious = node;
    spliceAfter.CurrentNext = node;

#if DEBUG
    Console.WriteLine(String.Join(", ", NodesCurrentOrder(currentHead).Take(maxPrint)));
#endif
}

var zero = ScanForwardTill(currentHead, value: 0);
int answer = ScanForwardMany(zero, 1000, 2000, 3000)
    .Select(n => n.Num)
    .Aggregate(0, (acc, num) => acc + num);

Console.WriteLine($"the answer is {answer} (took {sw.Elapsed})");

static Node ScanTill(Node node, Func<Node, Node> next, int value)
{
    while (true)
    {
        if (node.Num == value)
        {
            return node;
        }
        node = next(node);
    }
}

static Node ScanForwardTill(Node node, int value) => ScanTill(node, CurrentNext, value);

static IEnumerable<Node> ScanMany(Node node, Func<Node, Node> next, params int[] offsets)
{
    var set = offsets.ToHashSet();
    for (int i = 0; set.Count() > 0; i++)
    {
        if (set.Contains(i))
        {
            yield return node;
            set.Remove(i);
        }
        node = next(node);
    }
}

static IEnumerable<Node> ScanForwardMany(Node node, params int[] offsets) => ScanMany(node, CurrentNext, offsets);

static Node Scan(Node node, Func<Node, Node> next, int offset)
{
    while (offset > 0)
    {
        node = next(node);
        offset--;
    }
    return node;
}

static Node ScanForward(Node node, int offset) => Scan(node, CurrentNext, offset);

static Node ScanBackward(Node node, int offset) => Scan(node, CurrentPrevious, offset);

static IEnumerable<Node> Nodes(Node head, Func<Node, Node> next)
{
    var current = head;
    do
    {
        yield return current;
        current = next(current);
    } while (current != head);
}

static IEnumerable<Node> NodesOriginalOrder(Node head) => Nodes(head, OriginalNext);

static IEnumerable<Node> NodesCurrentOrder(Node head) => Nodes(head, CurrentNext);

static Node OriginalNext(Node node) => node.OriginalNext;

static Node CurrentNext(Node node) => node.CurrentNext;

static Node CurrentPrevious(Node node) => node.CurrentPrevious;

static Node BuildQuadruplyLinkedList(IEnumerable<int> input)
{
    Node? head = null, tail = null;

    foreach (int num in input)
    {
        var node = new Node { Num = num };

        if (head == null)
        {
            head = node;
        }

        if (tail != null)
        {
            tail.OriginalNext = tail.CurrentNext = node;
            node.OriginalPrevious = node.CurrentPrevious = tail;
        }

        tail = node;
    }

    // setup the list to loop around infinitely
    // head.previous => tail
    // tail.next => head
    head.OriginalPrevious = head.CurrentPrevious = tail;
    tail.OriginalNext = tail.CurrentNext = head;

    return head;
}

class Node // a node in a quadruply (!!!) linked list
{
    public Node? OriginalNext { get; set; }
    public Node? OriginalPrevious { get; set; }
    public Node? CurrentNext { get; set; }
    public Node? CurrentPrevious { get; set; }
    public int Num { get; init; }
    public override string ToString() => Num.ToString();
}
