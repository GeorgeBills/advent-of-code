using System.Text.RegularExpressions;

const string monkeyRegexStr = """
    Monkey (?<num>\d+):
      Starting items: ((?<item>\d+), )*(?<item>\d+)
      Operation: new = old (?<op>[\+\*]) (?<opval>(\d+|old))
      Test: divisible by (?<divby>\d+)
        If true: throw to monkey (?<iftrue>\d+)
        If false: throw to monkey (?<iffalse>\d+)
    """;

var monkeyRegex = new Regex(monkeyRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

string input = File.ReadAllText(args[0]);

var matches = monkeyRegex.Matches(input);
if (!matches.Any())
{
    throw new Exception("no matches");
}

var monkeys = matches
    .Select(
        match => new
        {
            num = int.Parse(match.Groups["num"].Value),
            items = match.Groups["item"].Captures.Select(c => ulong.Parse(c.Value)).ToList(),
            op = char.Parse(match.Groups["op"].Value),
            operatorValue = match.Groups["opval"].Value,
            divisibleBy = ulong.Parse(match.Groups["divby"].Value),
            throwToIfTrue = int.Parse(match.Groups["iftrue"].Value),
            throwToIfFalse = int.Parse(match.Groups["iffalse"].Value),
        }
    )
    .Select(
        parsed => new Monkey(
            Num: parsed.num,
            Items: parsed.items,
            Operation: newOperation(parsed.op, parsed.operatorValue),
            DivisibleBy: parsed.divisibleBy,
            ThrowToIfTrue: parsed.throwToIfTrue,
            ThrowToIfFalse: parsed.throwToIfFalse
        )
    )
    .ToArray();

ulong wrapBy = monkeys
    .Select(monkey => monkey.DivisibleBy)
    .Aggregate(1ul, (acc, db) => acc * db);

bool numbersMatchIndexes = monkeys
    .Select((monkey, index) => (monkey.Num, index))
    .All(ni => ni.Num == ni.index);

if (!numbersMatchIndexes)
{
    throw new Exception("mis-numbered monkeys!");
}

var monkeyInspectionCount = monkeys.ToDictionary(m => m.Num, m => 0ul);

const int numRounds = 10_000;

for (int round = 1; round <= numRounds; round++)
{
    foreach (var monkey in monkeys)
    {
        while (monkey.Items.Any())
        {
            // inspect
            ulong worry = monkey.Items.First();
            worry = monkey.Operation(worry);
            monkeyInspectionCount[monkey.Num] = monkeyInspectionCount[monkey.Num] + 1;

            // wrap worry level by the product of all of our divisors
            worry = worry % wrapBy;

            // throw to next monkey
            bool test = worry % monkey.DivisibleBy == 0;

            int throwToIndex = test ? monkey.ThrowToIfTrue : monkey.ThrowToIfFalse;
            var throwToMonkey = monkeys[throwToIndex];
            monkey.Items.RemoveAt(0);
            throwToMonkey.Items.Add(worry);
        }
    }

    if (round == 1 || round == 20 || round % 1000 == 0)
    {
        Console.WriteLine($"== After round {round} ==");
        foreach (int num in monkeyInspectionCount.Keys.OrderBy(n => n))
        {
            ulong inspectionCount = monkeyInspectionCount[num];
            Console.WriteLine($"Monkey {num} inspected items {inspectionCount} times.");
        }
    }
}

ulong monkeyBusiness = monkeyInspectionCount.Values
    .OrderByDescending(n => n)
    .Take(2)
    .Aggregate(1ul, (acc, ic) => acc * ic);

Console.WriteLine(monkeyBusiness);

Func<ulong, ulong> newOperation(char op, string operatorValue)
    => operatorValue == "old"
    ? newOldOperation(op)
    : newIntOperation(op, ulong.Parse(operatorValue));

Func<ulong, ulong> newOldOperation(char op) => op switch
{
    '+' => n => n + n,
    '*' => n => n * n,
    _ => throw new Exception($"operator {op} unhandled"),
};

Func<ulong, ulong> newIntOperation(char op, ulong operatorValue) => op switch
{
    '+' => n => n + operatorValue,
    '*' => n => n * operatorValue,
    _ => throw new Exception($"operator {op} unhandled"),
};

record Monkey(
    int Num,
    List<ulong> Items,
    Func<ulong, ulong> Operation,
    ulong DivisibleBy,
    int ThrowToIfTrue,
    int ThrowToIfFalse
);
