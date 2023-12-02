﻿using System.Text.RegularExpressions;

const string CubesRegex = "(?<num>\\d+) (?<colour>red|green|blue)";
const string SetRegex = $"(?<set>{CubesRegex}(, {CubesRegex})*)";
const string GameRegex = $"^Game (?<game_id>\\d+): {SetRegex}(; {SetRegex})*";

// "only 12 red cubes, 13 green cubes, and 14 blue cubes"
const int MaxRed = 12;
const int MaxGreen = 13;
const int MaxBlue = 14;

string file = args.Length >= 1 ? args[0] : "eg.txt";

var games = File.ReadAllLines(file).Select(line => ParseGame(line)).ToArray();
var possible = games.Where(g => g.IsPossible(MaxRed, MaxGreen, MaxBlue));
int answer = possible.Select(g => g.ID).Sum();

Console.WriteLine($"{possible.Count()} games are possible; the sum of their IDs is {answer}");

static Set ParseSet(string str)
{
    var match = Regex.Match(str, SetRegex, RegexOptions.ExplicitCapture);
    if (!match.Success)
    {
        throw new ArgumentException($"invalid set: {str}");
    }

    var nums = match.Groups["num"].Captures.Select(c => int.Parse(c.Value));
    var colours = match.Groups["colour"].Captures.Select(c => c.Value);
    var setdict = Enumerable.Zip(colours, nums).ToDictionary(x => x.Item1, x => x.Item2);
    return new Set(
        Red: setdict.GetValueOrDefault("red"),
        Green: setdict.GetValueOrDefault("green"),
        Blue: setdict.GetValueOrDefault("blue")
    );
}

static Game ParseGame(string str)
{
    var match = Regex.Match(str, GameRegex, RegexOptions.ExplicitCapture);
    if (!match.Success)
    {
        throw new ArgumentException($"invalid game: {str}");
    }

    int id = int.Parse(match.Groups["game_id"].Value);
    var sets = match.Groups["set"].Captures.Select(capture => ParseSet(capture.Value)).ToArray();
    return new Game(id, sets);
}

record Set(int Red, int Green, int Blue)
{
    public override string ToString() => $"{Red} red, {Green} green, {Blue} blue";
    public bool IsPossible(int MaxRed, int MaxGreen, int MaxBlue) => Red <= MaxRed && Green <= MaxGreen && Blue <= MaxBlue;
}

record Game(int ID, IEnumerable<Set> Sets)
{
    public override string ToString() => $"game {ID}: {string.Join("; ", Sets)}";
    public bool IsPossible(int MaxRed, int MaxGreen, int MaxBlue) => Sets.All(s => s.IsPossible(MaxRed, MaxGreen, MaxBlue));
}
