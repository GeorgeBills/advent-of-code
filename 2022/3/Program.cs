const int numItemTypes = 52; // a..z (26) + A..Z (26)

Func<char, int> itemTypeToPriority = ch => ch switch
{
    // "Lowercase item types a through z have priorities 1 through 26"
    >= 'a' and <= 'z' => (int)ch - (int)'a' + 1,
    // "Uppercase item types A through Z have priorities 27 through 52"
    >= 'A' and <= 'Z' => (int)ch - (int)'A' + 27,
    _ => throw new ArgumentException($"{ch} unhandled"),
};

Func<int, char> priorityToItemType = n => n switch
{
    >= 1 and <= 26 => (char)(n - 1 + (int)'a'),
    >= 27 and <= 52 => (char)(n - 27 + (int)'A'),
    _ => throw new ArgumentException($"{n} unhandled"),
};

Func<string, int[]> getItemTypeCount = str =>
{
    // take advantage of the fact that the priorities are all unique...
    // we could use an int64 and set bits given that we don't care about the count
    int[] itemTypePriorityCounts = new int[numItemTypes];
    foreach (char itemType in str)
    {
        int priority = itemTypeToPriority(itemType);
        itemTypePriorityCounts[priority - 1]++;
    }
    return itemTypePriorityCounts;
};

Func<string, string, (char itemType, int priority)?> getFirstCommonItemType = (c1Items, c2Items) =>
{
    int[] c1ItemTypes = getItemTypeCount(c1Items);
    int[] c2ItemTypes = getItemTypeCount(c2Items);
    for (int i = 0; i < numItemTypes; i++)
    {
        if (c1ItemTypes[i] > 0 && c2ItemTypes[i] > 0)
        {
            int priority = i + 1;
            char itemType = priorityToItemType(priority);
            return (itemType, priority);
        }
    }
    return null;
};

var rucksacks = File.ReadLines(args[0])
        .Select(line => { int mid = line.Length / 2; return (line[..mid], line[mid..]); })
        .Select(parsed => getFirstCommonItemType(parsed.Item1, parsed.Item2))
        .Sum(pnc => pnc.HasValue ? pnc.Value.priority : 0);
// .Select(parsed => (getItemTypePriorityCount(parsed.Item1), getItemTypePriorityCount(parsed.Item2)));

Console.WriteLine(string.Join(Environment.NewLine, rucksacks));

public readonly record struct Rucksack(char[] Compartment1, char[] Compartment2);
