Func<char, RPS> parseRPS = ch => ch switch
{
    'A' => RPS.Rock,
    'B' => RPS.Paper,
    'C' => RPS.Scissors,
    _ => throw new ArgumentException($"{ch} unrecognized"),
};

Func<char, WLD> parseWLD = ch => ch switch
{
    'X' => WLD.Loss,
    'Y' => WLD.Draw,
    'Z' => WLD.Win,
    _ => throw new ArgumentException($"{ch} unrecognized"),
};

Func<(RPS, RPS), int> scoreVs = choices => choices switch
{
    (RPS.Rock, RPS.Scissors) or (RPS.Paper, RPS.Rock) or (RPS.Scissors, RPS.Paper) => Score.Win,
    (RPS.Scissors, RPS.Rock) or (RPS.Rock, RPS.Paper) or (RPS.Paper, RPS.Scissors) => Score.Loss,
    var (a, b) when a == b => Score.Draw,
    _ => throw new ArgumentException($"{choices} unhandled"),
};

Func<RPS, WLD, RPS> playForResultVs = (opponentChoice, desiredResult) => (opponentChoice, desiredResult) switch
{
    (RPS.Rock, WLD.Win) => RPS.Paper,
    (RPS.Rock, WLD.Loss) => RPS.Scissors,
    (RPS.Rock, WLD.Draw) => RPS.Rock,
    (RPS.Paper, WLD.Win) => RPS.Scissors,
    (RPS.Paper, WLD.Loss) => RPS.Rock,
    (RPS.Paper, WLD.Draw) => RPS.Paper,
    (RPS.Scissors, WLD.Win) => RPS.Rock,
    (RPS.Scissors, WLD.Loss) => RPS.Paper,
    (RPS.Scissors, WLD.Draw) => RPS.Scissors,
    _ => throw new ArgumentException($"{opponentChoice} {desiredResult} unhandled"),
};

Func<RPS, int> scoreChoice = choice => choice switch
{
    RPS.Rock => 1,
    RPS.Paper => 2,
    RPS.Scissors => 3,
    _ => throw new ArgumentException($"{choice} unrecognized"),
};

int score = File.ReadLines(args[0])
    .Select(line => (theirChoice: parseRPS(line[0]), desiredResult: parseWLD(line[2])))
    .Select(parsed => (ourChoice: playForResultVs(parsed.theirChoice, parsed.desiredResult), parsed.theirChoice))
    .Select(choices => scoreVs(choices) + scoreChoice(choices.ourChoice))
    .Sum();

Console.WriteLine(score);

enum RPS { Rock, Paper, Scissors };

enum WLD { Win, Loss, Draw };

public static class Score
{
    public const int Win = 6;
    public const int Loss = 0;
    public const int Draw = 3;
}
