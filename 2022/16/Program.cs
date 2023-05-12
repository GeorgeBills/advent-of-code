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

var indexes = parsed.Select((p, idx) => (p.ID, idx)).ToDictionary(x => x.ID, x => x.idx);
int[] flows = parsed.Select(v => v.Flow).ToArray();
string[] valves = parsed.Select(v => v.ID).ToArray();
int[][] edges = parsed.Select(p => p.TunnelsTo.Select(t => indexes[t]).ToArray()).ToArray();

// consider only positive flow valves that haven't been opened
ulong consider = flows
    .Select((flow, idx) => (flow, idx))
    .Where(fi => fi.flow > 0)
    .Aggregate(0ul, (acc, fi) => acc | (1ul << fi.idx));

const string start = "AA"; // "start at valve AA"
const int time = 26;       // "leaving you with only 26 minutes"
int startIndex = indexes[start];

var memo = new Dictionary<State, int>();

var startState = new State(startIndex, startIndex, consider, time, us: true);

var sw = Stopwatch.StartNew();
int best = Search(startState);
sw.Stop();

Console.WriteLine($"maximum score is {best} (elapsed: {sw.Elapsed}; memoised: {memo.Count()})");

int Search(State state)
{
    int score = 0;

    if (state.time <= 0)
    {
        return score;
    }

    if (memo.TryGetValue(state, out score))
    {
        return score;
    }

    int best = int.MinValue;

    int node = state.us ? state.nodeUs : state.nodeEl;

    // search adjacent positions
    foreach (int adjacent in edges[node])
    {
        var newstate = state.us
            ? state with { nodeUs = adjacent, us = false }
            : state with { nodeEl = adjacent, us = true, time = state.time - 1 };
        score = Search(newstate);
        best = Math.Max(score, best);
    }

    // possibly open the valve at our current position
    bool openable = (state.consider & (1ul << node)) != 0;
    if (openable)
    {
        int released = (state.time - 1) * flows[node];      // increase our score
        ulong newconsider = state.consider ^ (1ul << node); // no longer consider the closed valve
        var newstate = state.us
            ? state with { consider = newconsider, us = false }
            : state with { consider = newconsider, us = true, time = state.time - 1 };
        score = released + Search(newstate);
        best = Math.Max(score, best);
    }

    memo[state] = best;

    return best;
}

readonly record struct State(int nodeUs, int nodeEl, ulong consider, int time, bool us);
