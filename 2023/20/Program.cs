using System.Text.RegularExpressions;

const string file = "in.txt";

const string broadcaster = "broadcaster";
const string output = "output";
const string rx = "rx";

var queue = new Queue<Pulse>(); // "always processed in the order they are sent"

var modules = File.ReadLines(file).Select(l => ParseModule(queue, l)).ToDictionary(m => m.Name, m => m);

// "register" the connections so that conjunction modules function properly
// "they initially default to remembering a low pulse for each input"
// otherwise if the first pulse is high then all pulses seen so far are high
foreach (var m in modules.Values)
{
    foreach (var d in m.Destinations)
    {
        if (modules.TryGetValue(d, out var n) && n is Conjunction)
        {
            m.ConnectTo(n);
        }
    }
}

var sent = new Dictionary<PulseType, int>();
for (int i = 0; ; i++)
{
#if DEBUG
    Console.WriteLine(i + 1);
#endif

    var initial = new Pulse(null, broadcaster, PulseType.Low);
    queue.Enqueue(initial);

    while (queue.TryDequeue(out var pulse))
    {
#if DEBUG
        Console.WriteLine(pulse);
#endif

        sent[pulse.Type] = sent.GetValueOrDefault(pulse.Type, 0) + 1;

        switch (pulse.To)
        {
            case output:
                Console.WriteLine($"output: {pulse.Type}");
                continue;
            case rx:
                if (pulse.Type == PulseType.Low)
                {
                    Console.WriteLine($"low pulse sent to {rx} after {i + 1} button presses");
                    goto DONE;
                }
                continue;
            default:
                var module = modules[pulse.To];
                module.HandlePulse(pulse);
                break;
        }

    }
#if DEBUG
    Console.WriteLine();
#endif
}

DONE:
int low = sent[PulseType.Low];
int high = sent[PulseType.High];
Console.WriteLine($"saw {low} low and {high} high for an answer of {low * high}");

static Module ParseModule(Queue<Pulse> queue, string modulestr)
{
    const string flipflop = "%";
    const string conjunction = "&";

    const string pattern = @"^((?<name>broadcaster)|((?<type>[%&])(?<name>[a-z]+))) -> (?<dest>\w+)(, (?<dest>\w+))*$";
    var match = Regex.Match(modulestr, pattern, RegexOptions.ExplicitCapture);
    if (!match.Success)
    {
        throw new ArgumentException($"couldn't parse module string: {modulestr}");
    }

    string name = match.Groups["name"].Value;
    string[] destinations = match.Groups["dest"].Captures.Select(c => c.Value).ToArray();

    return match.Groups["type"].Value switch
    {
        "" => new Broadcast(queue, name, destinations),
        flipflop => new FlipFlop(queue, name, destinations),
        conjunction => new Conjunction(queue, name, destinations),
    };
}

enum PulseType : sbyte { None = 0, Low = -1, High = 1 }

enum ModuleType { Broadcast, FlipFlop, Conjunction }

record Pulse(string? From, string To, PulseType Type)
{
    public override string ToString() => $"{From ?? "button"} -{Type.ToString().ToLower()}-> {To}";
}

abstract class Module
{
    public readonly string Name;
    public readonly IEnumerable<string> Destinations;

    private readonly Queue<Pulse> queue;

    public Module(Queue<Pulse> queue, string name, IEnumerable<string> destinations)
    {
        this.Name = name;
        this.Destinations = destinations;

        this.queue = queue;
    }

    protected void SendPulse(PulseType type)
    {
        foreach (var d in Destinations)
        {
            var pulse = new Pulse(this.Name, d, type);
            queue.Enqueue(pulse);
        }
    }

    public abstract void HandlePulse(Pulse transmission);

    public virtual void ConnectTo(Module m) => m.ConnectFrom(this);

    protected virtual void ConnectFrom(Module module) { /* do nothing */ }
}

class Broadcast : Module
{
    public Broadcast(Queue<Pulse> queue,
                    string name,
                    IEnumerable<string> destinations) : base(queue, name, destinations) { }

    // "sends the same pulse to all of its destination modules"
    public override void HandlePulse(Pulse transmission) => SendPulse(transmission.Type);
}

class FlipFlop : Module
{
    private enum FlipFlopState { On, Off }

    private FlipFlopState state;

    public FlipFlop(Queue<Pulse> queue,
                    string name,
                    IEnumerable<string> destinations) : base(queue, name, destinations)
    {
        this.state = FlipFlopState.Off; // "they are initially off"
    }

    public override void HandlePulse(Pulse transmission)
    {
        var (type, newstate) = transmission.Type switch
        {
            // "If a flip-flop module receives a high pulse, it is ignored and nothing happens."
            PulseType.High => (PulseType.None, state),

            // "If a flip-flop module receives a low pulse, it flips between on and off."
            PulseType.Low => state switch
            {
                FlipFlopState.On => (PulseType.Low, FlipFlopState.Off),  // "If it was on, it turns off and sends a low pulse."
                FlipFlopState.Off => (PulseType.High, FlipFlopState.On), // "If it was off, it turns on and sends a high pulse."
            },
        };

        this.state = newstate;

        if (type != PulseType.None)
        {
            SendPulse(type);
        }
    }
}

class Conjunction : Module
{
    private const PulseType defaultPulseType = PulseType.Low;

    // "remember the type of the most recent pulse received from each of their
    //  connected input modules"
    private readonly IDictionary<string, PulseType> received;

    public Conjunction(Queue<Pulse> queue,
                       string name,
                       IEnumerable<string> destinations) : base(queue, name, destinations)
    {
        this.received = new Dictionary<string, PulseType>();
    }

    public override void HandlePulse(Pulse transmission)
    {
        // "first updates its memory for that input"
        received[transmission.From] = transmission.Type;

        // "If it remembers high pulses for all inputs..."
        var type = received.Values.All(p => p == PulseType.High)
            ? PulseType.Low   // "it sends a low pulse"
            : PulseType.High; // "otherwise, it sends a high pulse"

        SendPulse(type);
    }

    protected override void ConnectFrom(Module m) => received[m.Name] = defaultPulseType;
}
