using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

var blueprints = ParseBlueprints(file);

var start = new State(
    Robots: new Resources(Ore: 1), // "you have exactly one ore-collecting robot in your pack"
    Resources: new Resources(),
    TimeRemaining: 24              // "after 24 minutes"
);

int qualities = 0;
foreach (var bp in blueprints)
{
    var searcher = new Searcher();
    int geodes = searcher.Search(bp, start);

    // "Determine the quality level of each blueprint by multiplying that
    // blueprint's ID number with the largest number of geodes that can be
    // opened in 24 minutes using that blueprint."
    int quality = geodes * bp.Number;

    Console.WriteLine($"best geode count for blueprint {bp.Number} is {geodes} (quality {quality})");
    qualities += quality;
}

Console.WriteLine($"the total quality is {qualities}");

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

#if DEBUG
    Console.WriteLine($"{blueprints.Count()} blueprints parsed");
    foreach (var bp in blueprints)
    {
        Console.WriteLine(bp);
    }
#endif

    return blueprints;
}

class Searcher
{
    private int best;
    private readonly IDictionary<State, int> memo;

    public Searcher()
    {
        this.best = 0;
        this.memo = new Dictionary<State, int>();
    }

    public int Search(Blueprint blueprint, State state)
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

        // calculate the best possible score we can get from this position.
        int possible =
            resources.Geode +
            (timeRemaining * robots.Geode) +             // all geode robots produce one geode every turn
            ((timeRemaining * (timeRemaining + 1)) / 2); // assume we build a geode robot every turn (very optimistic...)

        if (possible < best)
        {
            // we can't possibly beat our best known score. this branch of the
            // tree can be abandoned.
            return int.MinValue;
        }

        var successors = GenerateSuccessors(blueprint, state);

        int max = 0;
        foreach (var successor in successors)
        {
            int score = Search(blueprint, successor);
            max = Math.Max(max, score);
        }

        memo[state] = max;

        return best;
    }

    private static IEnumerable<State> GenerateSuccessors(Blueprint blueprint, State state)
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

        if (resourcesBefore.CanAfford(blueprint.GeodeRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Geode = robots.Geode + 1 },
                Resources = resourcesAfter.Subtract(blueprint.GeodeRobotCosts),
            };
        }

        if (resourcesBefore.CanAfford(blueprint.ObsidianRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Obsidian = robots.Obsidian + 1 },
                Resources = resourcesAfter.Subtract(blueprint.ObsidianRobotCosts),
            };
        }

        if (resourcesBefore.CanAfford(blueprint.ClayRobotCosts))
        {
            yield return stateAfter with
            {
                Robots = robots with { Clay = robots.Clay + 1 },
                Resources = resourcesAfter.Subtract(blueprint.ClayRobotCosts),
            };
        }

        if (resourcesBefore.CanAfford(blueprint.OreRobotCosts))
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

record Blueprint(int Number, Resources OreRobotCosts, Resources ClayRobotCosts, Resources ObsidianRobotCosts, Resources GeodeRobotCosts);

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
