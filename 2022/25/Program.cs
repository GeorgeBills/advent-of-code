using System.Diagnostics;
using System.Text;

string file = args.Length == 1 ? args[0] : "eg.txt";

long sum = File.ReadLines(file)
    .Select(line => ParseSnafu(line))
    .Sum();

string snafu = ToSnafu(sum);

Console.WriteLine($"the sum of all SNAFU numbers is {snafu}");

static string ToSnafu(long n)
{
    var sb = new StringBuilder();

    Debug.WriteLine($"converting {n} to SNAFU");

    int logfloor = (int)Math.Floor(Math.Log(n, 5));

    for (int i = logfloor; i >= 0; i--)
    {
        long radix = (long)Math.Pow(5, i);
        (long digit, n) = Math.DivRem(n, radix);

        Debug.WriteLine($"{new { digit, radix, remainder = n }}");

        char c = (char)(digit + '0');
        sb.Append(c);
    }

    return sb.ToString();
}

static long ParseSnafu(string snafu)
{
    long n = 0;

    for (int i = snafu.Length - 1; i >= 0; i--)
    {
        // "SNAFU works the same way, except it uses powers of five instead of
        // ten. Starting from the right, you have a ones place, a fives place, a
        // twenty-fives place, a one-hundred-and-twenty-fives place, and so on."
        long radix = (long)Math.Pow(5, snafu.Length - i - 1);

        // "Instead of using digits four through zero, the digits are 2, 1, 0,
        // minus (written -), and double-minus (written =).""
        char c = snafu[i];
        int digit = c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '-' => -1, // "minus is worth -1"
            '=' => -2, // "double-minus is worth -2"
            _ => throw new ArgumentException($"unmatched snafu character: {c}"),
        };

        n += (digit * radix);
    }

    return n;
}
