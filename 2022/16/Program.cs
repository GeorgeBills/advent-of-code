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

int numNodes = parsed.Count();
var indexes = parsed.Select((p, idx) => (p.ID, idx)).ToDictionary(x => x.ID, x => x.idx);
int[] flows = parsed.Select(v => v.Flow).ToArray();
string[] valves = parsed.Select(v => v.ID).ToArray();
int[][] edges = parsed.Select(p => p.TunnelsTo.Select(t => indexes[t]).ToArray()).ToArray();
int[,] costs = FloydWarshall(edges);

// consider only positive flow valves that haven't been opened
ulong consider = flows
    .Select((flow, idx) => (flow, idx))
    .Where(fi => fi.flow > 0)
    .Aggregate(0ul, (acc, fi) => acc | (1ul << fi.idx));

const string start = "AA"; // "start at valve AA"
const int time = 30;       // "leaving you with only 26 minutes"
int startIndex = indexes[start];

var memo = new Dictionary<State, int>();

var startState = new State(startIndex, consider, time);

var sw = Stopwatch.StartNew();
int best = Search(startState);
sw.Stop();

Console.WriteLine($"maximum score is {best} (elapsed: {sw.Elapsed}; memoised: {memo.Count()})");

int Search(State state)
{
    if (memo.TryGetValue(state, out int cachedScore))
    {
        return cachedScore;
    }

    int best = 0;
    for (int valveidx = 0; valveidx < numNodes; valveidx++)
    {
        int cost = costs[state.node, valveidx] + 1; // +1 to actually open the valve
        if (cost > state.time)
        {
            // can't afford to move to and open this valve
            continue;
        }

        bool openable = (state.consider & (1ul << valveidx)) != 0;
        if (!openable)
        {
            // either already open or zero flow rate so no point opening
            continue;
        }

        // consider moving to and opening the valve
        int released = (state.time - cost) * flows[valveidx];
        int score = Search(
            state with
            {
                node = valveidx,                               // move to the valve
                consider = state.consider ^ (1ul << valveidx), // close the valve
                time = state.time - cost,
            }
        );
        best = Math.Max(score + released, best);
    }

    memo[state] = best;

    return best;
}

int[,] FloydWarshall(int[][] edges)
{
    int[,] costs = new int[numNodes, numNodes]; // cost from i to j

    // set initial costs
    for (int i = 0; i < numNodes; i++)
    {
        for (int j = 0; j < numNodes; j++)
        {
            if (i == j) // self
            {
                costs[i, j] = 0;
            }
            else if (edges[i].Contains(j)) // edge with weight 1 (direct path)
            {
                costs[i, j] = 1;
            }
            else // no edge: infinity (for now)
            {
                costs[i, j] = int.MaxValue;
            }
        }
    }

    // iteratively refne costs including nodes from sets [], [1], [1,2], [1,2,...k].
    for (int k = 0; k < numNodes; k++)
    {
        for (int i = 0; i < numNodes; i++)
        {
            for (int j = 0; j < numNodes; j++)
            {
                int current = costs[i, j];
                int withk = costs[i, k] != int.MaxValue && costs[k, j] != int.MaxValue
                    ? costs[i, k] + costs[k, j]
                    : int.MaxValue;
                // if withk is less than current, then it's faster to go from i
                // to j via k (i => k => j) compared to our current cheapest
                // path, and withk is thus our new cheapest path
                costs[i, j] = Math.Min(current, withk);
            }
        }
    }

#if DEBUG // print our cost matrix
    Console.Write("{0,3}", String.Empty);
    for (int j = 0; j < numNodes; j++)
    {
        Console.Write("{0,3}", valves[j]);
    }
    Console.WriteLine();
    for (int i = 0; i < numNodes; i++)
    {
        Console.Write("{0,3}", valves[i]);
        for (int j = 0; j < numNodes; j++)
        {
            Console.Write("{0,3:D}", costs[i, j] == int.MaxValue ? '∞' : costs[i, j]);
        }
        Console.WriteLine();
    }
#endif

    return costs;
}

readonly record struct State(int node, ulong consider, int time);
