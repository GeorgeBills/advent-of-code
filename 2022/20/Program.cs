string file = args.Length == 1 ? args[0] : "eg.txt";

var input = File.ReadLines(file)
    .Select((line, i) => (num: int.Parse(line), idx: i))
    .ToArray();

DebugPrint(input);

Console.WriteLine(new { input.Length });

for (int i = 0; i < input.Length; i++)
{
    (int num, int idx) = input[i];

    // how many positions to move the number by
    int move = mod(num, input.Length);

    // the index of the position we're moving to
    int newidx = mod(idx + move, input.Length);

    // BUG BUG BUG negative numbers moving one less than they should

    Console.WriteLine($"moving {input[i].num} at position {input[i].idx} by {move} positions to position {newidx}");

    if (idx != newidx)
    {
        // reindex all numbers in between our start and finish indexes
        // if the current num is moving up then they all move down, else they all move up
        (int lower, int upper, int shift) = idx < newidx
            ? (idx + 1, newidx, -1)
            : (newidx, idx - 1, +1);

        // Console.WriteLine(new { lower, upper, shift });

        for (int j = 0; j < input.Length; j++)
        {
            if (lower <= input[j].idx && input[j].idx <= upper)
            {
                Console.WriteLine($"moving {input[j].num} at position {input[j].idx} to position {input[j].idx + shift}");
                input[j].idx += shift;
            }
        }

        // move the number to the new position
        input[i].idx = newidx;
    }

    DebugPrint(input);
}

// https://torstencurdt.com/tech/posts/modulo-of-negative-numbers/
static int mod(int a, int b) => (((a % b) + b) % b);

static void InvariantOrThrow(IEnumerable<(int num, int idx)> input)
{
    const int max = 10;
    var expected = Enumerable.Range(0, input.Count());
    var actual = input.Select(np => np.idx).Order();
    if (!Enumerable.SequenceEqual(expected, actual))
    {
        string ellipsis = expected.Count() > max ? "..." : "";
        throw new Exception($"expected {String.Join(',', expected.Take(max)) + ellipsis} did not match actual {String.Join(',', actual.Take(max)) + ellipsis}");
    }
}

static void DebugPrint(IEnumerable<(int num, int idx)> input)
{
#if DEBUG
    InvariantOrThrow(input);

    var sorted = input.OrderBy(np => np.idx);
    var nums = sorted.Select(np => np.num);
    Console.WriteLine(String.Join(", ", nums));
#endif
}
