using System.Diagnostics;

const int maxPrint = 20;
const int decryptionKey = 811589153; // "you need to apply the decryption key, 811589153"
const int rounds = 10;               // "you need to mix the list of numbers ten times"

string file = args.Length >= 1 ? args[0] : "eg.txt";
bool check = args.Length == 2 && args[1] == "--check";

var input = File.ReadLines(file)
    .Select(long.Parse)
    .Select(num => num * decryptionKey); // "multiply each number by the decryption key before you begin"

// TODO: use sentinel node?

var first = BuildQuadruplyLinkedList(input);
var originalHead = first;
var currentHead = first;

#if DEBUG
Console.WriteLine("initial");
Console.WriteLine(String.Join(", ", NodesCurrentOrder(currentHead).Take(maxPrint)));
#endif

var sw = Stopwatch.StartNew();
for (int i = 0; i < rounds; i++)
{
    Console.WriteLine($"round {i + 1}");

    foreach (var node in NodesOriginalOrder(originalHead))
    {
        long offset = (int)(node.Num % (input.Count() - 1)); // don't include this node in the mod!

// #if DEBUG
//         Console.WriteLine($"processing {node}; offset: {offset:+#;-#;0}");
// #endif

        long offsetShouldBe = node.Num;

        if (offset == 0)
        {
            continue;
        }

        // remove node from list
        node.CurrentNext.CurrentPrevious = node.CurrentPrevious;
        node.CurrentPrevious.CurrentNext = node.CurrentNext;
        if (node == currentHead)
        {
            currentHead = node.CurrentNext;
        }

        // reinsert node
        Node spliceAfter;

        // if check is set we process splice after the simple (but incredibly slow) way
        // then we compare the result we get the known correct way vs the modulus way
        // if the answers differ, then the modulus way is incorrect
        Node? spliceAfterShouldBe = null;

        if (offset > 0)
        {
            spliceAfter = ScanForward(node, offset);
            if (check)
            {
                spliceAfterShouldBe = ScanForward(node, offsetShouldBe);
            }
        }
        else
        {
            offset = Math.Abs(offset) + 1; // plus one because we're including ourselves
            spliceAfter = ScanBackward(node, offset);
            if (check)
            {
                spliceAfterShouldBe = ScanBackward(node, Math.Abs(offsetShouldBe) + 1);
            }
        }

        if (check && spliceAfter != spliceAfterShouldBe)
        {
            throw new Exception($"should splice after {spliceAfterShouldBe} (offset {offsetShouldBe}); instead we are splicing after {spliceAfter} (offset {offset})");
        }

// #if DEBUG
//         Console.WriteLine($"splicing in at {spliceAfter.CurrentPrevious} => {spliceAfter} => {node}* => {spliceAfter.CurrentNext}");
// #endif

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

// #if DEBUG
//         Console.WriteLine(String.Join(", ", NodesCurrentOrder(currentHead).Take(maxPrint)));
// #endif
    }

#if DEBUG
    Console.WriteLine(String.Join(", ", NodesCurrentOrder(currentHead).Take(maxPrint)));
#endif
}

var zero = ScanForwardTill(currentHead, value: 0);
var nodes = ScanForwardMany(zero, 1000, 2000, 3000).ToArray();
long answer = nodes
    .Select(n => n.Num)
    .Aggregate(0L, (acc, num) => acc + num);

Console.WriteLine($"the answer is {answer} (n1: {nodes[0].Num}; n2: {nodes[1].Num}; n3: {nodes[2].Num}; took {sw.Elapsed})");

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
    int prev = 0;
    foreach (int offset in offsets)
    {
        // offsets are all relative to the start nod. so subtract the previous
        // offset from the current offset to get the offset relative to the node
        // where we last yielded.
        int actual = offset - prev;
        node = ScanForward(node, offset - prev);
        yield return node;
        prev = offset;
    }
}

static IEnumerable<Node> ScanForwardMany(Node node, params int[] offsets) => ScanMany(node, CurrentNext, offsets);

static Node Scan(Node node, Func<Node, Node> next, long offset)
{
    while (offset > 0)
    {
        node = next(node);
        offset--;
    }
    return node;
}

static Node ScanForward(Node node, long offset) => Scan(node, CurrentNext, offset);

static Node ScanBackward(Node node, long offset) => Scan(node, CurrentPrevious, offset);

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

static Node BuildQuadruplyLinkedList(IEnumerable<long> input)
{
    Node? head = null, tail = null;

    foreach (long num in input)
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
    public long Num { get; init; }
    public override string ToString() => Num.ToString();
}
