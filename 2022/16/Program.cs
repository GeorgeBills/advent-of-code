using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

var lineRegex = new Regex(
    @"^Valve (?<valve>[A-Z]{2}) has flow rate=(?<flow>\d+); tunnels? leads? to valves? ((?<to>[A-Z]{2})(, )?)+",
    RegexOptions.ExplicitCapture
);

var valves = File.ReadLines(file)
    .Select(line => lineRegex.Match(line).Groups)
    .Select(groups =>
        new
        {
            ID = groups["valve"].Value,
            Flow = int.Parse(groups["flow"].Value),
            TunnelsTo = groups["to"].Captures.Select(c => c.Value).ToArray(),
        }
    );

Console.WriteLine($"parsed {valves.Count()} valves");
// Console.WriteLine(String.Join(Environment.NewLine, valves));

var flows = valves.ToDictionary(v => v.ID, v => v.Flow);
var edges = valves.SelectMany(v => v.TunnelsTo.Select(t => (from: v.ID, to: t))).ToLookup(t => t.from);
var prune = valves.Where(v => v.Flow == 0).Select(v => v.ID).ToHashSet();

foreach (var e in edges)
{
    Console.WriteLine($"{e.Key}: {String.Join(',', e.Select(e => e.to).Order())}");
}

Console.WriteLine($"pruned: {String.Join(',', prune)}");

foreach (string s in valves.Select(v => v.ID))
{
    var c = Connected(s, new HashSet<string>());
    Console.WriteLine($"connected {s}: {String.Join(',', c)}");
}

const string start = "AA";

var state = new State { Position = start, Pruned = prune, Flows = flows };
var sw = Stopwatch.StartNew();
var best = Search(state);
sw.Stop();

Console.WriteLine($"maximum score is {best} (elapsed: {sw.Elapsed})");

int Search(State state)
{
    if (state.Depth >= State.MaxDepth)
    {
        return state.Score;
    }

    int best = int.MinValue;
    var moves = GenerateMoves(state);
    foreach (var move in moves)
    {
        move.MakeMove(state);
        int score = Search(state);
        if (score > best)
        {
            best = score;
        }
        move.UnmakeMove(state);
    }
    return best;
}

IEnumerable<IMove> GenerateMoves(State state)
{
    yield return new MoveOpenValve(state.Position);

    var connected = Connected(state.Position, new HashSet<string>());
    var visitable = connected.Where(c => (state.Depth + c.Cost) <= State.MaxDepth);

    foreach (var v in visitable)
    {
        yield return new MoveThroughTunnel(From: state.Position, To: v.Node, v.Cost);
    }
}

IEnumerable<(string Node, int Cost)> Connected(string valve, ISet<string> seen, int weight = 1)
{
    seen.Add(valve);
    foreach (var edge in edges[valve])
    {
        string to = edge.to;
        if (seen.Contains(to))
        {
            break;
        }
        if (!prune.Contains(to))
        {
            yield return (to, weight); // edge directly connects valve => to
        }
        else
        {
            foreach (var transitive in Connected(to, seen, weight + 1))
            {
                yield return transitive; // path from valve => transitive
            }
        }
    }
}

interface IMove
{
    void MakeMove(State state);
    void UnmakeMove(State state);
}

record MoveThroughTunnel(string From, string To, int Cost) : IMove
{
    public void MakeMove(State state)
    {
        state.Position = To;
        state.Depth += Cost;
    }

    public void UnmakeMove(State state)
    {
        state.Position = From;
        state.Depth -= Cost;
    }
}

record MoveOpenValve(string Valve) : IMove
{
    public void MakeMove(State state)
    {
        state.Pruned.Add(Valve);
        state.Score += Score(state.Flows[Valve], state.Depth);
        state.Depth += 1;
    }

    public void UnmakeMove(State state)
    {
        state.Pruned.Remove(Valve);
        state.Depth -= 1;
        state.Score -= Score(state.Flows[Valve], state.Depth);
    }

    private int Score(int flow, int depth) => flow * (State.MaxDepth - depth);
}

class State
{
    public const int MaxDepth = 15;
    public string Position;
    public int Score = 0;
    public int Depth = 1;
    public ISet<string> Pruned;

    public IDictionary<string, int> Flows; // dirty hack to put this on the state class
};
