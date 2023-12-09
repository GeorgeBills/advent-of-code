using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

var histories = File
    .ReadLines(file)
    .Select(line => Regex.Matches(line, @"\d+"))
    .Select(matches => matches.Select(m => m.Value).Select(int.Parse).ToArray())
    .ToArray();

int[] extrapolated = new int[histories.Length];
for (int i = 0; i < histories.Length; i++)
{
    Console.WriteLine($"history {i + 1}");

    var arrs = new List<(int[] Values, int? Placeholder)> { (histories[i], null) };
    Console.WriteLine(string.Join(',', arrs.Last().Values));

    // calculate differences
    while (!arrs.Last().Values.All(n => n == 0))
    {
        var diff = Difference(arrs.Last().Values);
        arrs.Add((diff, null));
        Console.WriteLine(string.Join(',', arrs.Last().Values));
    }

    // fill in placeholders
    var arrsarr = arrs.ToArray();
    arrsarr[arrsarr.Length - 1].Placeholder = 0;
    for (int j = arrsarr.Length - 2; j >= 0; j--)
    {
        arrsarr[j].Placeholder = arrsarr[j].Values.Last() + arrsarr[j + 1].Placeholder;
    }
    Console.WriteLine(string.Join(',', arrsarr.Select(valph => valph.Placeholder)));

    extrapolated[i] = (int)arrsarr[0].Placeholder!;

    Console.WriteLine();
}

Console.WriteLine($"extrapolated {string.Join(',', extrapolated.Take(10))}... for a total of {extrapolated.Sum()}");

static int[] Difference(int[] arr)
{
    int[] difference = new int[arr.Length - 1];
    for (int i = 0; i < arr.Length - 1; i++)
    {
        difference[i] = arr[i + 1] - arr[i];
    }
    return difference;
}
