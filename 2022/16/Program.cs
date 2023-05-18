using System.Diagnostics;
using System.Numerics;
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

// consider only positive flow valves
var nonzeroidxs = flows
    .Select((flow, idx) => (flow, idx))
    .Where(fi => fi.flow > 0)
    .Select(fi => fi.idx);

var pairs = PowerSet(nonzeroidxs).Where(s => s.Count() > 0).Select(s => (s, nonzeroidxs.Except(s)));

ulong nonzero = nonzeroidxs.Aggregate(0ul, (acc, idx) => acc | (1ul << idx));

Console.WriteLine($"{pairs.Count()} total pairs to search");

const string start = "AA"; // "start at valve AA"
const int time = 26;       // "leaving you with only 26 minutes"
int startIndex = indexes[start];

var memo = new Dictionary<State, int>();

var sw = Stopwatch.StartNew();

var best = pairs
    .Select(
        pair => (
            usOpened: pair.Item1.Aggregate(0ul, (acc, idx) => acc | (1ul << idx)),
            elOpened: pair.Item2.Aggregate(0ul, (acc, idx) => acc | (1ul << idx))
        )
    )
    .Select(
        pair => (
            pair.usOpened,
            usStartState: new State(startIndex, opened: pair.usOpened, time),
            pair.elOpened,
            elStartState: new State(startIndex, opened: pair.elOpened, time)
        )
    )
    .Select(
        pair => (
            pair.usOpened,
            usScore: Search(pair.usStartState),
            pair.elOpened,
            elScore: Search(pair.elStartState)
        )
    )
    .Select(pair =>
        new
        {
            pair.usScore,
            usExplored = String.Join(',', ToValves(nonzero & ~pair.usOpened)),
            pair.elScore,
            elExplored = String.Join(',', ToValves(nonzero & ~pair.elOpened)),
            total = pair.usScore + pair.elScore
        }
    )
    .MaxBy(pair => pair.total);

sw.Stop();

Console.WriteLine($"{best} (elapsed: {sw.ElapsedMilliseconds}ms; memoised: {memo.Count()})");

/*
 * []      => []
 * [x]     => [], [x]
 * [x,y]   => [], [x], [y], [x,y]
 * [x,y,z] => [], [x], [y], [x,y], [z], [y,z], [x,z], [x,y,z] 
 * [y,z]   => [],      [y],        [z], [y,z]
 */
IEnumerable<IEnumerable<T>> PowerSet<T>(IEnumerable<T> set)
{
    yield return new T[] { };

    var first = set.FirstOrDefault();
    var remainder = set.Skip(1);

    if (set.Count() == 0)
    {
        yield break;
    }

    if (set.Count() >= 1)
    {
        yield return new T[] { first! };
    }

    if (set.Count() >= 2)
    {
        var powerset = PowerSet(remainder).Where(s => s.Count() > 0);
        foreach (var s in powerset)
        {
            yield return s;
            yield return s.Prepend(first!);
        }
    }
}

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

        bool consider = (~state.opened & nonzero & (1ul << valveidx)) != 0;
        if (!consider)
        {
            // either already open or zero flow rate so no point opening
            continue;
        }

        // consider moving to and opening the valve
        int released = (state.time - cost) * flows[valveidx];
        int score = Search(
            state with
            {
                node = valveidx,                           // move to the valve
                opened = state.opened | (1ul << valveidx), // open the valve
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

IEnumerable<string> ToValves(ulong idxs)
{
    while (idxs != 0)
    {
        string bitstr = Convert.ToString((long)idxs, 2);
        int idx = BitOperations.TrailingZeroCount(idxs);
        idxs ^= (1ul << idx);
        yield return valves[idx];
    }
}

readonly record struct State(int node, ulong opened, int time);
