using System.Diagnostics;
using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

// "Unfortunately, one of the elephants ate most of your blueprint list! Now,
// only the first three blueprints in your list are intact."
var blueprints = ParseBlueprints(file).Take(3);

#if DEBUG
Console.WriteLine($"{blueprints.Count()} blueprints parsed");
foreach (var bp in blueprints)
{
    Console.WriteLine(bp);
}
#endif

var start = new State(
    Robots: new Resources(Ore: 1), // "you have exactly one ore-collecting robot in your pack"
    Resources: new Resources(),
    TimeRemaining: 32              // "you figure you probably have 32 minutes"
);

// "You no longer have enough blueprints to worry about quality levels. Instead,
// for each of the first three blueprints, determine the largest number of
// geodes you could open; then, multiply these three values together."
int answer = 1;
var sw = Stopwatch.StartNew();
foreach (var bp in blueprints)
{
    var searcher = new Searcher(bp);
    int geodes = searcher.Search(start);
    Console.WriteLine($"best geode count for blueprint {bp.Number} is {geodes}");
    answer *= geodes;
}

Console.WriteLine($"the answer is {answer} (took {sw.Elapsed})");

static IEnumerable<Blueprint> ParseBlueprints(string file)
{
    string input = File.ReadAllText(file);

    string regexstr = @"Blueprint[ ](?<num>\d+):\s+
                        Each[ ]ore[ ]robot[ ]costs[ ](?<ore_costs_ore>\d+)[ ]ore.\s*
                        Each[ ]clay[ ]robot[ ]costs[ ](?<clay_costs_ore>\d+)[ ]ore.\s*
                        Each[ ]obsidian[ ]robot[ ]costs[ ](?<obsidian_costs_ore>\d+)[ ]ore[ ]and[ ](?<obsidian_costs_clay>\d+)[ ]clay.\s*
                        Each[ ]geode[ ]robot[ ]costs[ ](?<geode_costs_ore>\d+)[ ]ore[ ]and[ ](?<geode_costs_obsidian>\d+)[ ]obsidian.";

    var regex = new Regex(regexstr,
                          RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    var blueprints =
        regex.Matches(input)
        .Select(match => new
        {
            num = int.Parse(match.Groups["num"].Value),
            ore_costs_ore = int.Parse(match.Groups["ore_costs_ore"].Value),
            clay_costs_ore = int.Parse(match.Groups["clay_costs_ore"].Value),
            obsidian_costs_ore = int.Parse(match.Groups["obsidian_costs_ore"].Value),
            obsidian_costs_clay = int.Parse(match.Groups["obsidian_costs_clay"].Value),
            geode_costs_ore = int.Parse(match.Groups["geode_costs_ore"].Value),
            geode_costs_obsidian = int.Parse(match.Groups["geode_costs_obsidian"].Value),
        })
        .Select(
            parsed => new Blueprint(
                Number: parsed.num,
                OreRobotCosts: new Resources(Ore: parsed.ore_costs_ore),
                ClayRobotCosts: new Resources(Ore: parsed.clay_costs_ore),
                ObsidianRobotCosts: new Resources(Ore: parsed.obsidian_costs_ore, Clay: parsed.obsidian_costs_clay),
                GeodeRobotCosts: new Resources(Ore: parsed.geode_costs_ore, Obsidian: parsed.geode_costs_obsidian)
            )
        );

    return blueprints;
}

class Searcher
{
    private int best;
    private readonly IDictionary<State, int> memo;
    private readonly Blueprint blueprint;
    private readonly int maxOre;
    private readonly int maxClay;
    private readonly int maxObsidian;

    public Searcher(Blueprint blueprint)
    {
        this.best = 0;
        this.memo = new Dictionary<State, int>();
        this.blueprint = blueprint;

        var costs = blueprint.Costs();
        this.maxOre = costs.Select(c => c.Ore).Max();
        this.maxClay = costs.Select(c => c.Clay).Max();
        this.maxObsidian = costs.Select(c => c.Obsidian).Max();
    }

    public int Search(State state)
    {
        if (memo.TryGetValue(state, out int memoised))
        {
            return memoised;
        }

        var (robots, resources, timeRemaining) = state;

        if (timeRemaining == 0)
        {
            int score = resources.Geode;
            best = Math.Max(best, score);
            return score;
        }

        // can we afford a geode robot right now?
        bool canAffordNow = resources.CanAfford(blueprint.GeodeRobotCosts);

        // can we afford one geode robot each and every turn?
        bool canAffordSustained = robots.CanAfford(blueprint.GeodeRobotCosts);

        // to build a sustained one geode robot a turn we need...
        var required = blueprint.GeodeRobotCosts.Subtract(robots);

        int obsidianBy = MinimumTurnsTillResource(required.Obsidian, resources.Obsidian, robots.Obsidian);
        int oreBy = MinimumTurnsTillResource(required.Ore, resources.Ore, robots.Ore);
        int earliestSustainedBy = Math.Max(obsidianBy, oreBy);

        // calculate the best possible score we can get from this position.
        // this is hopefully the lowest guess we can make without ever underestimating.
        int possible =
            resources.Geode +
            (canAffordNow && !canAffordSustained ? timeRemaining : 0) + // if we build a geode robot now we'll get timeRemaining geodes from it
            (timeRemaining * robots.Geode) +     // all geode robots produce one geode every turn
            Sum1N(timeRemaining - earliestSustainedBy);   // assume we build a geode robot every turn

        if (possible < best)
        {
            // we can't possibly beat our best known score.
            // this branch of the tree can be abandoned.
            return int.MinValue;
        }

        var successors = GenerateSuccessors(blueprint, state);

        int max = 0;
        foreach (var successor in successors)
        {
            int score = Search(successor);
            max = Math.Max(max, score);
        }

        memo[state] = max;

        return best;

        // assuming we build one robot / turn, the amount of resources produced
        // by those robots is the sum from 1 to N.
        //
        // the robot built on the 1st turn produces one resource multiplied by N turns,
        // the robot built on the 2nd turn produces one resource multiplied by N-1 turns,
        // the robot built on the 3rd turn produces one resource multiplied by N-2 turns,
        // the robot built on the Nth turn produces one resource multiplied by 1 turns.
        static int Sum1N(int n) => n * (n + 1) / 2;

        // given resources and robots in hand, what is the minimum number of
        // turns till we can achieve the required resource count?
        static int MinimumTurnsTillResource(int requiredResource, int startingResource, int startingNumRobots) =>
            Enumerable
                .Range(1, int.MaxValue)
                .Select(turn => (
                    turn,
                    totalResource:
                        startingResource +         // the amount we start with
                        turn * startingNumRobots + // our robots produce one per turn 
                        Sum1N(turn)                // assume we build one robot per turn
                    )
                )
                .First(tr => tr.totalResource >= requiredResource)
                .turn;
    }

    private IEnumerable<State> GenerateSuccessors(Blueprint blueprint, State state)
    {
        var (robots, resourcesBefore, timeRemaining) = state;

        // "Each robot can collect 1 of its resource type per minute."
        var resourcesAfter = resourcesBefore.Add(robots);

        var stateAfter = state with
        {
            Resources = resourcesAfter,
            TimeRemaining = timeRemaining - 1,
        };

        // search the "deeper in the tech tree" robots first (i.e. be greedy) in
        // the hopes that it'll lead to faster cutoffs.

        // don't consider states where we already have enough robots to produce
        // the max of a resource we can spend (maxOre, maxObsidian, maxClay) and
        // we would build another one. that robot type is maxed out, there's no
        // benefit to be gained.

        if (resourcesBefore.CanAfford(blueprint.GeodeRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Geode = robots.Geode + 1 },
                Resources = resourcesAfter.Subtract(blueprint.GeodeRobotCosts),
            };
        }

        if (robots.Obsidian < this.maxObsidian && resourcesBefore.CanAfford(blueprint.ObsidianRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Obsidian = robots.Obsidian + 1 },
                Resources = resourcesAfter.Subtract(blueprint.ObsidianRobotCosts),
            };
        }

        if (robots.Clay < this.maxClay && resourcesBefore.CanAfford(blueprint.ClayRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Clay = robots.Clay + 1 },
                Resources = resourcesAfter.Subtract(blueprint.ClayRobotCosts),
            };
        }

        if (robots.Ore < this.maxOre && resourcesBefore.CanAfford(blueprint.OreRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Ore = robots.Ore + 1 },
                Resources = resourcesAfter.Subtract(blueprint.OreRobotCosts),
            };
        }

        yield return stateAfter; // not building any robots
    }
}

record Blueprint(int Number, Resources OreRobotCosts, Resources ClayRobotCosts, Resources ObsidianRobotCosts, Resources GeodeRobotCosts)
{
    public IEnumerable<Resources> Costs() => new[] { OreRobotCosts, ClayRobotCosts, ObsidianRobotCosts, GeodeRobotCosts };
}

record Resources(int Ore = 0, int Clay = 0, int Obsidian = 0, int Geode = 0)
{
    public bool CanAfford(Resources costs) =>
        Ore >= costs.Ore &&
        Clay >= costs.Clay &&
        Obsidian >= costs.Obsidian &&
        Geode >= costs.Geode;

    public Resources Add(Resources add) => this with
    {
        Ore = Ore + add.Ore,
        Clay = Clay + add.Clay,
        Obsidian = Obsidian + add.Obsidian,
        Geode = Geode + add.Geode,
    };

    public Resources Subtract(Resources subtract) => this with
    {
        Ore = Ore - subtract.Ore,
        Clay = Clay - subtract.Clay,
        Obsidian = Obsidian - subtract.Obsidian,
        Geode = Geode - subtract.Geode,
    };
}

record State(Resources Robots, Resources Resources, int TimeRemaining);
