

string file = args.Length >= 1 ? args[0] : "eg.txt";

var comparer = new Comparer();

int winnings = File.ReadLines(file)
    .Select(ParseLine)
    .OrderBy(
        keySelector: hb => { var (hand, _) = hb; return (hand, Evaluate(hand)); },
        comparer
    )
    .Select((hb, idx) => hb.bid * (idx + 1))
    .Sum();

Console.WriteLine($"total winnings are {winnings}");

const int Cards = 13;

static Strength Evaluate((int, int, int, int, int) hand)
{
    Span<int> counts = stackalloc int[Cards];
    counts[hand.Item1 - 1]++;
    counts[hand.Item2 - 1]++;
    counts[hand.Item3 - 1]++;
    counts[hand.Item4 - 1]++;
    counts[hand.Item5 - 1]++;

    int have2 = 0;
    int have3 = 0;
    int have4 = 0;
    for (int i = 1; i < Cards; i++)
    {
        if (counts[i] == 5) { return Strength.Kind5; }
        if (counts[i] == 4) { have4++; continue; }
        if (counts[i] == 3) { have3++; continue; }
        if (counts[i] == 2) { have2++; continue; }
    }

    int jokers = counts[0];
    return (jokers, have2, have3, have4) switch
    {
        (0, 0, 0, 0) => Strength.High,
        (0, 0, 0, 1) => Strength.Kind4,
        (0, 0, 1, 0) => Strength.Kind3,
        (0, 1, 0, 0) => Strength.Pair1,
        (0, 1, 1, 0) => Strength.House,
        (0, 2, 0, 0) => Strength.Pair2,
        (1, 0, 0, 0) => Strength.Pair1,
        (1, 0, 0, 1) => Strength.Kind5,
        (1, 0, 1, 0) => Strength.Kind4,
        (1, 1, 0, 0) => Strength.Kind3,
        (1, 2, 0, 0) => Strength.House,
        (2, 0, 0, 0) => Strength.Kind3,
        (2, 0, 1, 0) => Strength.Kind5,
        (2, 1, 0, 0) => Strength.Kind4,
        (3, 0, 0, 0) => Strength.Kind4,
        (3, 1, 0, 0) => Strength.Kind5,
        (4, 0, 0, 0) => Strength.Kind5,
        (5, 0, 0, 0) => Strength.Kind5,
        _ => throw new ArgumentException($"unhandled hand: {hand}"),
    };
}

static ((int, int, int, int, int) hand, int bid) ParseLine(string line)
{
    var split = line.Split(' ');
    var hand = ParseHand(split[0]);
    int bid = int.Parse(split[1]);
    return (hand, bid);
}

static (int, int, int, int, int) ParseHand(string str) => (
    ParseCard(str[0]),
    ParseCard(str[1]),
    ParseCard(str[2]),
    ParseCard(str[3]),
    ParseCard(str[4])
);

const int J = 1;
const int T = 10;
const int Q = 11;
const int K = 12;
const int A = 13;

static int ParseCard(char c) => c switch
{
    >= '2' and <= '9' => c - '0',
    'T' => T,
    'J' => J,
    'Q' => Q,
    'K' => K,
    'A' => A,
    _ => throw new ArgumentException($"invalid card: {c}"),
};

enum Strength { High, Pair1, Pair2, Kind3, House, Kind4, Kind5 };

class Comparer : IComparer<((int, int, int, int, int) hand, Strength strength)>
{
    public int Compare(((int, int, int, int, int) hand, Strength strength) a,
                       ((int, int, int, int, int) hand, Strength strength) b)
    {
        var (ahand, astrength) = a;
        var (bhand, bstrength) = b;
        if (astrength != bstrength) return astrength.CompareTo(bstrength);
        if (ahand.Item1 != bhand.Item1) return ahand.Item1.CompareTo(bhand.Item1);
        if (ahand.Item2 != bhand.Item2) return ahand.Item2.CompareTo(bhand.Item2);
        if (ahand.Item3 != bhand.Item3) return ahand.Item3.CompareTo(bhand.Item3);
        if (ahand.Item4 != bhand.Item4) return ahand.Item4.CompareTo(bhand.Item4);
        if (ahand.Item5 != bhand.Item5) return ahand.Item5.CompareTo(bhand.Item5);
        return 0;
    }
}
