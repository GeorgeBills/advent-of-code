using System.Data;
using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

string input = File.ReadAllText(file);
var seeds = ParseSeeds(input);
var maps = ParseMaps(input);

#if DEBUG
Console.WriteLine(string.Join(',', seeds));
foreach (var map in maps)
{
    Console.WriteLine(string.Join(',', map));
}
#endif

var locations = seeds.Select(seed => MapLocation(seed, maps));

#if DEBUG
Console.WriteLine(string.Join(',', locations));
#endif

Console.WriteLine($"the lowest location is {locations.Min()}");

static uint MapLocation(uint seed, IEnumerable<(uint dst, uint src, uint len)>[] maps)
{
    uint n = seed;
    foreach (var map in maps)
    {
        foreach (var (dst, src, len) in map)
        {
            uint off = n - src;
            if (0 <= off && off < len)
            {
                n = dst + off;
                break;
            }
        }
    }
    return n;
}

static IEnumerable<uint> ParseSeeds(string input)
{
    const string seedsregex = @"seeds:( (?<seed>\d+))+";
    var seedmatch = Regex.Match(input, seedsregex, RegexOptions.ExplicitCapture);
    return seedmatch.Groups["seed"].Captures.Select(c => c.Value).Select(uint.Parse).ToArray();
}

static IEnumerable<(uint dst, uint src, uint len)>[] ParseMaps(string input)
{
    const string mapregex = @"\w+-to-\w+ map:(\n(?<dst>\d+) (?<src>\d+) (?<len>\d+))+";
    var mapmatches = Regex.Matches(input, mapregex, RegexOptions.ExplicitCapture);
    return mapmatches.Select(ParseMapMatch).ToArray(); // relying on these being in order
}

static IEnumerable<(uint dst, uint src, uint len)> ParseMapMatch(Match match)
{
    var dsts = match.Groups["dst"].Captures.Select(c => c.Value).Select(uint.Parse);
    var srcs = match.Groups["src"].Captures.Select(c => c.Value).Select(uint.Parse);
    var lens = match.Groups["len"].Captures.Select(c => c.Value).Select(uint.Parse);
    var map = Enumerable.Zip(dsts, srcs, lens).ToArray();
    return map;
}
