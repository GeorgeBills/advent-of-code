const int rucksacksPerTeam = 3;

const int minItemTypePriority = 1;
const int maxItemTypePriority = 52;

var getPriority = (char itemType) => itemType switch
{
    // "Lowercase item types a through z have priorities 1 through 26"
    >= 'a' and <= 'z' => (int)itemType - (int)'a' + 1,
    // "Uppercase item types A through Z have priorities 27 through 52"
    >= 'A' and <= 'Z' => (int)itemType - (int)'A' + 27,
    _ => throw new ArgumentException($"{itemType} unhandled"),
};

var getItemType = (int priority) => priority switch
{
    >= 1 and <= 26 => (char)(priority + (int)'a' - 1),
    >= 27 and <= 52 => (char)(priority + (int)'A' - 27),
    _ => throw new ArgumentException($"{priority} unhandled"),
};

var getPriorityBit = (int priority) => 1ul << priority - 1;

var getItemTypePresence = (string rucksack) =>
    rucksack.ToCharArray()
        .Select(itemType => getPriority(itemType))
        .Select(priority => getPriorityBit(priority))
        .Aggregate(0ul, (presence, priorityBit) => presence | priorityBit);

var getFirstCommonItemType = (ulong[] itemTypePresence) =>
    Enumerable.Range(minItemTypePriority, maxItemTypePriority)
        .Select(priority => (priority, priorityBit: getPriorityBit(priority)))
        .First(ppb => itemTypePresence.All(presence => (presence & ppb.priorityBit) != 0))
        .priority;

var rucksacks = File.ReadLines(args[0]);

if (rucksacks.Count() % rucksacksPerTeam != 0)
{
    throw new Exception($"require {rucksacksPerTeam} rucksacks per team");
}

int answer = rucksacks
    .Select(rs => getItemTypePresence(rs))          // check which item types are in each rucksack
    .Chunk(rucksacksPerTeam)                        // group rucksacks by team
    .Select(chunk => getFirstCommonItemType(chunk)) // priority of first common item type for each team
    .Sum();

Console.WriteLine(answer);
