using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

int points =
    File.ReadAllLines(file).Select(ParseCard)
    .Select(card => CountWinningNumbers(card.winning, card.have))
    .Select(n => CalculatePoints(n))
    .Sum();

Console.WriteLine($"the cards are worth {points} total points");

static Card ParseCard(string str)
{
    const string cardregex = @"^
                                Card\s+\d+:\s+            # Card N:
                                ((?<winning_num>\d+)\s*)+ # winning numbers 
                                \s+\|\s+                  # | delimiter
                                ((?<have_num>\d+)\s*)+    # have numbers
                               $";

    var match = Regex.Match(str, cardregex, RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
    if (!match.Success)
    {
        throw new ArgumentException($"invalid card: {str}");
    }

    var winning = match.Groups["winning_num"].Captures.Select(c => int.Parse(c.Value)).ToHashSet();
    var have = match.Groups["have_num"].Captures.Select(c => int.Parse(c.Value)).ToHashSet();
    return new Card(winning, have);
}

static int CountWinningNumbers(ISet<int> winning, ISet<int> have) => have.Count(h => winning.Contains(h));

static int CalculatePoints(int n) => n > 0 ? (int)Math.Pow(2, n - 1) : 0;

record Card(ISet<int> winning, ISet<int> have);
