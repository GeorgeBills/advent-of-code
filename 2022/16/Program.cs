using System.Diagnostics;
using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

var lineRegex = new Regex(
    @"^Valve (?<valve>[A-Z]{2}) has flow rate=(?<flow>\d+); tunnels? leads? to valves? ((?<to>[A-Z]{2})(, )?)+",
    RegexOptions.ExplicitCapture
);

var parsed = File.ReadLines(file)
    .Select(line => lineRegex.Match(line).Groups)
    .Select(groups =>
        new
        {
            ID = groups["valve"].Value,
            Flow = int.Parse(groups["flow"].Value),
            TunnelsTo = groups["to"].Captures.Select(c => c.Value).ToArray(),
        }
    );

int numValves = parsed.Count();
int[] flows = parsed.Select(v => v.Flow).ToArray();
string[] valves = parsed.Select(v => v.ID).ToArray();
bool[,] edges = new bool[numValves, numValves];

foreach (var valve in parsed)
{
    int nodeIndex = Array.FindIndex(valves, id => id == valve.ID);
    foreach (string tunnel in valve.TunnelsTo)
    {
        int adjacentIndex = Array.FindIndex(valves, id => id == tunnel);
        edges[nodeIndex, adjacentIndex] = true;
    }
}

// consider only positive flow valves that haven't been opened
ulong consider = flows
    .Select((flow, idx) => (flow, idx))
    .Where(fi => fi.flow > 0)
    .Aggregate(0ul, (acc, fi) => acc | (1ul << fi.idx));

const string start = "AA";
const int time = 30;
int startIndex = Array.FindIndex(valves, id => id == start);

var memo = new Dictionary<(int, ulong, int), int>();

var sw = Stopwatch.StartNew();
int best = Search(startIndex, consider);
sw.Stop();

Console.WriteLine($"maximum score is {best} (elapsed: {sw.Elapsed}; memoised: {memo.Count()})");

int Search(int node, ulong consider, int time = time)
{
    int score = 0;

    if (time <= 0)
    {
        return score;
    }

    if (memo.TryGetValue((node, consider, time), out score))
    {
        return score;
    }

    int best = int.MinValue;

    // search adjacent positions
    for (int adjacent = 0; adjacent < numValves; adjacent++)
    {
        if (edges[node, adjacent])
        {
            score = Search(adjacent, consider, time - 1);
            best = Math.Max(score, best);
        }
    }

    // possibly open the valve at our current position
    if ((consider & (1ul << node)) != 0)
    {
        int released = (time - 1) * flows[node];
        score = released + Search(node, consider ^ (1ul << node), time - 1);
        best = Math.Max(score, best);
    }

    memo[(node, consider, time)] = best;

    return best;
}
