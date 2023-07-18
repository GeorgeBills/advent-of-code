using System.Diagnostics;

// https://en.wikipedia.org/wiki/Balanced_ternary
// (except we actually have balanced quinary)

string file = args.Length == 1 ? args[0] : "eg.txt";

long sum = File.ReadLines(file)
    .Select(line => ParseSnafu(line))
    .Sum();

string snafu = ToSnafu(sum);

const int radix = 5;

Console.WriteLine($"the sum of all SNAFU numbers is {snafu}");

static string ToSnafu(long n)
{
    Debug.WriteLine($"converting {n} to SNAFU");

    // the largest exponent for a place we can divide the number by
    int logfloor = (int)Math.Floor(Math.Log(n, radix));

    // +1 digit for the 0's place
    // +1 digit to leave a leading zero for the balancing
    int numdigits = logfloor + 2;
    var digits = new int[numdigits];

    // calculate digits
    long remainder = n;
    for (int i = logfloor; i >= 0; i--)
    {
        long mult = (long)Math.Pow(radix, i);
        (long digit, remainder) = Math.DivRem(remainder, mult);

        // most significant digit is the 1th element
        // 0th element is left as zero so we have a digit to carry into
        int digitidx = logfloor - i + 1;
        digits[digitidx] = (int)digit;

        Debug.WriteLine($"{digitidx}{OrdinalSuffix(digitidx)} digit ({mult}s place) is {digit}; {digit} * {mult} = {digit * mult} leaving {remainder}");
    }

    // balance
    Debug.WriteLine($"digits before balancing: {String.Join(',', digits)}");
    bool balanced = false;
    while (!balanced)
    {
        balanced = true; // assume we balance succesfully in this loop
        for (int i = 0; i < digits.Length; i++)
        {
            // "03" => "1=" (1 * 5^1 - 2 * 5^0 = 5 - 2)   
            // "04" => "1-" (1 * 5^1 - 1 * 5^0 = 5 - 1)   
            while (digits[i] > 2)
            {
                digits[i] -= radix;
                digits[i - 1]++;
                balanced = false; // preceding digit may now need more balancing
            }
        }
    }
    Debug.WriteLine($"digits after balancing: {String.Join(',', digits)}");

    // convert to a string
    string snafu = new string(digits.SkipWhile(d => d == 0).Select(SnafuDigitToChar).ToArray());

    Debug.WriteLine($"original decimal number was {n}; SNAFU is {snafu}");

    Debug.Assert(ParseSnafu(snafu) == n, "SNAFU string doesn't round trip back to the input number...");

    return snafu;
}

static string OrdinalSuffix(int n) => n switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

static long ParseSnafu(string snafu)
{
    long n = 0;

    for (int i = snafu.Length - 1; i >= 0; i--)
    {
        // "SNAFU works the same way, except it uses powers of five instead of
        // ten. Starting from the right, you have a ones place, a fives place, a
        // twenty-fives place, a one-hundred-and-twenty-fives place, and so on."
        long mult = (long)Math.Pow(radix, snafu.Length - i - 1);

        char c = snafu[i];
        int digit = SnafuCharToDigit(c);

        n += digit * mult;
    }

    return n;
}

// "Instead of using digits four through zero, the digits are 2, 1, 0, minus
// (written -), and double-minus (written =)."

static int SnafuCharToDigit(int c) => c switch
{
    '0' => 0,
    '1' => 1,
    '2' => 2,
    '-' => -1, // "minus is worth -1"
    '=' => -2, // "double-minus is worth -2"
    _ => throw new ArgumentException($"unmatched SNAFU character: {c}"),
};

static char SnafuDigitToChar(int digit) => digit switch
{
    0 => '0',
    1 => '1',
    2 => '2',
    -1 => '-',
    -2 => '=',
    _ => throw new ArgumentOutOfRangeException($"{digit} is not a valid SNAFU digit"),
};
