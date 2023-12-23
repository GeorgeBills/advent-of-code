using System.Text.RegularExpressions;

const string file = "eg.txt";

const string broadcaster = "broadcaster";

var modules = File.ReadLines(file).Select(ParseModule).ToArray();

var indexes = modules.Select((m, i) => (m.Name, Index: i)).ToDictionary(ni => ni.Name, ni => ni.Index);

var types = modules.Select(m => m.Type).ToArray();

Console.WriteLine("types: " + string.Join(',', types));

int[][] outputs = modules.Select(m => m.Destinations.Select(d => indexes[d]).ToArray()).ToArray();

Console.WriteLine("outputs: " + string.Join(';', outputs.Select(op => string.Join(',', op))));

var inputslu = outputs.SelectMany((outidxs, modidx) => outidxs.Select(outidx => (outidx, modidx))).ToLookup(x => x.outidx, x => x.modidx);
int[][] inputs = Enumerable.Range(0, outputs.Length).Select(i => inputslu[i].ToArray()).ToArray();

Console.WriteLine("inputs: " + string.Join(';', inputs.Select(ip => string.Join(',', ip))));

var pulses = new Pulse[modules.Count(), modules.Count()];

var broadcasteri = indexes[broadcaster];

foreach (var outputi in outputs[broadcasteri])
{
    pulses[broadcasteri, outputi] = Pulse.Low;
}

// "Flip-flop modules (prefix %) are either on or off; they are initially off."
static (Pulse pulse, FlipFlopState state) FlipFlop(Pulse pulse, FlipFlopState state = FlipFlopState.Off) => pulse switch
{
    // "If a flip-flop module receives a high pulse, it is ignored and nothing happens."
    Pulse.High => (Pulse.None, state),

    // "If a flip-flop module receives a low pulse, it flips between on and off."
    Pulse.Low => state switch
    {
        FlipFlopState.On => (Pulse.Low, FlipFlopState.Off),  // "If it was on, it turns off and sends a low pulse."
        FlipFlopState.Off => (Pulse.High, FlipFlopState.On), // "If it was off, it turns on and sends a high pulse."
    },
};

// "Conjunction modules (prefix &) remember the type of the most recent pulse
//  received from each of their connected input modules; they initially default
//  to remembering a low pulse for each input. When a pulse is received, the
//  conjunction module first updates its memory for that input. Then, if it
//  remembers high pulses for all inputs, it sends a low pulse; otherwise, it
//  sends a high pulse."
static Pulse Conjunction(params Pulse[] input) => input.All(p => p == Pulse.High) ? Pulse.Low : Pulse.High;

static (string Name, ModuleType Type, IEnumerable<string> Destinations) ParseModule(string modulestr)
{
    const string tflipflop = "%";
    const string tconjunction = "&";

    const string pattern = @"^((?<name>broadcaster)|((?<type>[%&])(?<name>[a-z]+))) -> (?<dest>\w+)(, (?<dest>\w+))*$";
    var match = Regex.Match(modulestr, pattern, RegexOptions.ExplicitCapture);
    if (!match.Success)
    {
        throw new ArgumentException($"couldn't parse module string: {modulestr}");
    }
    return (
        match.Groups["name"].Value,
        match.Groups["type"].Value switch { "" => ModuleType.Broadcast, tflipflop => ModuleType.FlipFlop, tconjunction => ModuleType.Conjunction },
        match.Groups["dest"].Captures.Select(c => c.Value).ToArray()
    );
}

enum Pulse : sbyte { None = 0, Low = -1, High = 1 }

enum ModuleType { Broadcast, FlipFlop, Conjunction }

enum FlipFlopState { On, Off }
