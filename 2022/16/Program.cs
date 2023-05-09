using System.Diagnostics;
using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

var lineRegex = new Regex(
    @"^Valve (?<valve>[A-Z]+) has flow rate=(?<flow>\d+); tunnels? leads? to valves? ((?<to>[A-Z]+)(, )?)+",
    RegexOptions.ExplicitCapture
);

var valves = File.ReadLines(file)
    .Select(line => lineRegex.Match(line).Groups)
    .Select(groups =>
        new Valve(
            ID: groups["valve"].Value,
            Flow: int.Parse(groups["flow"].Value),
            TunnelsTo: groups["to"].Captures.Select(c => c.Value).ToArray()
        )
    )
    .ToDictionary(valve => valve.ID, valve => valve);

Console.WriteLine($"parsed {valves.Count()} valves");
// Console.WriteLine(String.Join(Environment.NewLine, valves));

const string start = "AA"; // "You start at valve AA...";
const int maxDepth = 30; // "...you have 30 minutes before the volcano erupts"

var state = new State(valves[start], valves);

var sw = Stopwatch.StartNew();
var best = Search(state);
sw.Stop();

Console.WriteLine($"maximum score is {best.score} (elapsed: {sw.Elapsed})");
Console.WriteLine(String.Join(Environment.NewLine, best.pv));

(IEnumerable<IMove> pv, int score) Search(State state)
{
    if (state.Depth >= maxDepth)
    {
        int score = state.Score(maxDepth);
        return (Enumerable.Empty<IMove>(), score);
    }

    var best = (
        move: (IMove?)null,
        pv: (IEnumerable<IMove>?)null,
        score: int.MinValue
    );

    var moves = GenerateMoves(state);
    foreach (var move in moves)
    {
        move.MakeMove(state);
        state.Depth++;

        (var pv, int score) = Search(state);
        if (score > best.score)
        {
            best = (move, pv.Prepend(move), score);
        }

        move.UnmakeMove(state);
        state.Depth--;
    }

    return (best.pv!, best.score);
}

IEnumerable<IMove> GenerateMoves(State state)
{
    if (!state.IsValveOpen() && state.PositionFlow > 0)
    {
        yield return new MoveOpenValve(state.PositionID);
    }

    foreach (var tunnel in state.PositionTunnelsTo)
    {
        yield return new MoveThroughTunnel(state.PositionID, tunnel);
    }
}

interface IMove
{
    void MakeMove(State state);
    void UnmakeMove(State state);
};

record struct MoveThroughTunnel(string From, string To) : IMove
{
    public void MakeMove(State state) => state.MoveTo(To);
    public void UnmakeMove(State state) => state.MoveTo(From);
    public override string ToString() => $"{From}=>{To}";
}

record struct MoveOpenValve(string Valve) : IMove
{
    public void MakeMove(State state) => state.OpenValve();
    public void UnmakeMove(State state) => state.CloseValve();
    public override string ToString() => $"open {Valve}";
}

class State
{
    public int Depth { get; set; }

    private Valve Position;

    private readonly IDictionary<string, Valve> Valves;
    private readonly IDictionary<string, int> Opened;

    public State(Valve position, Dictionary<string, Valve> valves)
    {
        this.Position = position;
        this.Opened = new Dictionary<string, int>();
        this.Valves = valves;
        this.Depth = 0;
    }

    public string PositionID { get => Position.ID; }
    public int PositionFlow { get => Position.Flow; }
    public IEnumerable<string> PositionTunnelsTo { get => Position.TunnelsTo; }

    public void MoveTo(string id) => Position = Valves[id];
    public void OpenValve() => Opened[Position.ID] = Depth;
    public void CloseValve() => Opened.Remove(Position.ID);
    public bool IsValveOpen() => Opened.ContainsKey(Position.ID);
    public int Score(int maxDepth) => Opened.Select(vd => Valves[vd.Key].Flow * (maxDepth - vd.Value)).Sum();
}

record struct Valve(string ID, int Flow, IEnumerable<string> TunnelsTo)
{
    public override string ToString() => $"{ID} ({Flow}); tunnels to: {String.Join(',', TunnelsTo)}";
}
