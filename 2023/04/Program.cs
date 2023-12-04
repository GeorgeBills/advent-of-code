using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

var cards = File.ReadAllLines(file).Select(ParseCard).ToArray();
var matches = cards.Select(card => CountMatchingNumbers(card.winning, card.have)).ToArray();

// part one
{
    int points = matches.Select(n => CalculatePoints(n)).Sum();
    Console.WriteLine($"the cards are worth {points} total points");
}

// part two
{
    int[] copies = new int[matches.Length];
    Array.Fill(copies, 1);

    for (int i = 0; i < matches.Length; i++)
    {
        int m = matches[i];

#if DEBUG
        Console.WriteLine($"card {i + 1} wins {m} copies");
#endif

        while (m > 0)
        {
            copies[i + m] += copies[i];
            m--;
        }
    }

#if DEBUG
    Console.WriteLine(string.Join(',', copies));
#endif

    Console.WriteLine($"there are {copies.Sum()} total cards");
}

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

static int CountMatchingNumbers(ISet<int> winning, ISet<int> have) => have.Intersect(winning).Count();

static int CalculatePoints(int n) => n > 0 ? (int)Math.Pow(2, n - 1) : 0;

record Card(ISet<int> winning, ISet<int> have);
