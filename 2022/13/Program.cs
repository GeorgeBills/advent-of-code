string file = args.Length == 1 ? args[0] : "eg.txt";

var packetPairs = File.ReadLines(file)
    .Chunk(3)
    .Select(chunk => chunk.Take(2))
    .Select(chunk => chunk.Select(r => Parse.PacketList(r)))
    .Select(chunk => (chunk.First(), chunk.Last()));

foreach (var packetPair in packetPairs)
{
    (var left, var right) = packetPair;
    Console.WriteLine(left);
    Console.WriteLine(right);
    Console.WriteLine();
}

int answer = packetPairs
    .Select(
        (pair, index) =>
        {
            (var left, var right) = pair;
            bool isRightOrder = left.CompareTo(right) <= 0;
            return new { pair, index, isRightOrder };
        }
    )
    .Where(pio => pio.isRightOrder)
    .Select(pio => pio.index + 1)
    .Sum();

Console.WriteLine(answer);

static class Parse
{
    public static PacketList PacketList(string str) => PacketList(new StringReader(str));

    public static PacketList PacketList(TextReader reader)
    {
        if ((char)reader.Read() != '[')
        {
            throw new Exception("expected '['");
        }

        var packets = new List<Packet>();

        char c = (char)reader.Peek();
        while (c != ']')
        {
            Packet packet = c switch
            {
                '[' => Parse.PacketList(reader),              // consume e.g. [1,2,3]
                >= '0' and <= '9' => Parse.PacketInt(reader), // consume e.g. 123
                _ => throw new Exception($"unexpected '{c}'"),
            };

            packets.Add(packet);

            // expecting either ',' or ']'
            c = (char)reader.Peek();

            if (c == ',')
            {
                reader.Read();           // consume ','
                c = (char)reader.Peek(); // peek next
            }
        }
        reader.Read(); // consume trailing ']'

        return new PacketList(packets.ToArray());
    }

    public static PacketInt PacketInt(TextReader reader)
    {
        if (!char.IsAsciiDigit((char)reader.Peek()))
        {
            throw new Exception("expected digit");
        }

        int n = 0;
        while (char.IsAsciiDigit((char)reader.Peek()))
        {
            int digit = (char)reader.Read() - '0';
            checked
            {
                n = 10 * n + digit;
            }
        }

        return new PacketInt(n);
    }
}

abstract record Packet() : IComparable<Packet>
{
    public int CompareTo(Packet? other) => (this, other) switch
    {
        (PacketInt thisPI, PacketInt otherPI) => thisPI.CompareTo(otherPI),
        (PacketList thisPL, PacketList otherPL) => thisPL.CompareTo(otherPL),

        // "If exactly one value is an integer, convert the integer to a list
        // which contains that integer as its only value, then retry the
        // comparison. For example, if comparing [0,0,0] and 2, convert the
        // right value to [2] (a list containing 2); the result is then found by
        // instead comparing [0,0,0] and [2]."
        (PacketInt thisPI, PacketList otherPL) => (new PacketList(thisPI)).CompareTo(otherPL),
        (PacketList thisPL, PacketInt otherPI) => thisPL.CompareTo(new PacketList(otherPI)),

        _ => throw new Exception($"unsupported comparison: {this} vs {other}"),
    };
}

record PacketInt(int N) : Packet, IComparable<PacketInt>
{
    public override string ToString() => N.ToString();

    // "If both values are integers, the lower integer should come first. If the
    // left integer is lower than the right integer, the inputs are in the right
    // order. If the left integer is higher than the right integer, the inputs
    // are not in the right order. Otherwise, the inputs are the same integer;
    // continue checking the next part of the input."
    public int CompareTo(PacketInt? other) => other != null ? N.CompareTo(other.N) : 1;
}

record PacketList(params Packet[] List) : Packet, IComparable<PacketList>
{
    public override string ToString() => "[" + String.Join(',', (IEnumerable<Packet>)List) + "]";

    public int Length { get => List.Length; }

    public Packet this[int i] { get => List[i]; }

    // "If both values are lists, compare the first value of each list, then the
    // second value, and so on. If the left list runs out of items first, the
    // inputs are in the right order. If the right list runs out of items first,
    // the inputs are not in the right order. If the lists are the same length
    // and no comparison makes a decision about the order, continue checking the
    // next part of the input."
    public int CompareTo(PacketList? other)
    {
        if (other == null) return 1;

        for (int i = 0; i < Math.Min(this.Length, other.Length); i++)
        {
            int compare = this[i].CompareTo(other[i]);
            if (compare != 0)
            {
                return compare;
            }
        }

        return this.Length.CompareTo(other.Length);
    }
}
