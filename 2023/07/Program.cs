

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
    counts[hand.Item1 - 2]++;
    counts[hand.Item2 - 2]++;
    counts[hand.Item3 - 2]++;
    counts[hand.Item4 - 2]++;
    counts[hand.Item5 - 2]++;

    int have2 = 0;
    int have3 = 0;
    for (int i = 0; i < Cards; i++)
    {
        if (counts[i] == 5) { return Strength.Kind5; }
        if (counts[i] == 4) { return Strength.Kind4; }
        if (counts[i] == 3) { have3++; continue; }
        if (counts[i] == 2) { have2++; continue; }
    }

    return (have2, have3) switch
    {
        (1, 1) => Strength.House,
        (1, 0) => Strength.Pair1,
        (2, 0) => Strength.Pair2,
        (0, 0) => Strength.High,
        (0, 1) => Strength.Kind3,
        _ => throw new Exception(), // impossible
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

const int T = 10;
const int J = 11;
const int Q = 12;
const int K = 13;
const int A = 14;

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
