using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

var histories =
    File.ReadLines(file)
    .Select(line => Regex.Matches(line, @"\d+"))
    .Select(matches => matches.Select(m => m.Value).Select(int.Parse).ToArray())
    .ToArray();

int[] extrapolated = new int[histories.Length];
for (int i = 0; i < histories.Length; i++)
{
    Console.WriteLine($"history {i + 1}");

    var arrs = new List<int[]> { histories[i] };
    Console.WriteLine(string.Join(',', arrs.Last()));

    // calculate differences
    while (!arrs.Last().All(n => n == 0))
    {
        var diff = Difference(arrs.Last());
        arrs.Add(diff);
        Console.WriteLine(string.Join(',', arrs.Last()));
    }

    // fill in placeholders
    var placeholders = new int[arrs.Count];
    placeholders[^1] = 0;
    for (int j = placeholders.Length - 2; j >= 0; j--)
    {
        placeholders[j] = arrs[j].Last() + placeholders[j + 1];
    }
    Console.WriteLine($"placeholders: {string.Join(',', placeholders.Reverse())}");

    extrapolated[i] = placeholders[0];

    Console.WriteLine();
}

Console.WriteLine($"extrapolated {string.Join(',', extrapolated)} for a total of {extrapolated.Sum()}");

static int[] Difference(int[] arr)
{
    int[] difference = new int[arr.Length - 1];
    for (int i = 0; i < arr.Length - 1; i++)
    {
        difference[i] = arr[i + 1] - arr[i];
    }
    return difference;
}
