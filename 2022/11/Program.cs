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
            items = match.Groups["item"].Captures.Select(c => int.Parse(c.Value)).ToList(),
            op = char.Parse(match.Groups["op"].Value),
            operatorValue = match.Groups["opval"].Value,
            divisibleBy = int.Parse(match.Groups["divby"].Value),
            throwToIfTrue = int.Parse(match.Groups["iftrue"].Value),
            throwToIfFalse = int.Parse(match.Groups["iffalse"].Value),
        }
    )
    .Select(
        parsed => new Monkey(
            Num: parsed.num,
            Items: parsed.items,
            Operation: newOperation(parsed.op, parsed.operatorValue),
            Test: newTestOperation(parsed.divisibleBy),
            ThrowToIfTrue: parsed.throwToIfTrue,
            ThrowToIfFalse: parsed.throwToIfFalse
        )
    )
    .ToArray();

bool numbersMatchIndexes = monkeys
    .Select((monkey, index) => (monkey.Num, index))
    .All(ni => ni.Num == ni.index);

if (!numbersMatchIndexes)
{
    throw new Exception("mis-numbered monkeys!");
}

var monkeyInspectionCount = monkeys.ToDictionary(m => m.Num, m => 0);

const double worryDivisor = 3.0;

for (int round = 1; round <= 20; round++)
{
    foreach (var monkey in monkeys)
    {
        Console.WriteLine($"Monkey {monkey.Num}");
        while (monkey.Items.Any())
        {
            // inspect
            int worry = monkey.Items.First();
            Console.WriteLine($"  Monkey inspects an item with a worry level of {worry}.");
            worry = monkey.Operation(worry);
            Console.WriteLine($"  Worry level is updated to {worry}");
            monkeyInspectionCount[monkey.Num] = monkeyInspectionCount[monkey.Num] + 1;

            // "After each monkey inspects an item but before it tests your
            // worry level, your relief that the monkey's inspection didn't
            // damage the item causes your worry level to be divided by three
            // and rounded down to the nearest integer."
            worry = (int)Math.Floor(worry / worryDivisor);
            Console.WriteLine($"  Monkey gets bored with item. Worry level is divided by {worryDivisor} to {worry}.");

            // throw to next monkey
            bool test = monkey.Test(worry);
            Console.WriteLine($"  Current worry level {(test ? "is" : "is not")} divisible.");

            int throwToIndex = test ? monkey.ThrowToIfTrue : monkey.ThrowToIfFalse;
            var throwToMonkey = monkeys[throwToIndex];
            monkey.Items.RemoveAt(0);
            throwToMonkey.Items.Add(worry);

            Console.WriteLine($"  Item with worry level {worry} is thrown to monkey {throwToIndex}.");
        }
    }

    Console.WriteLine($"After round {round}");
    foreach (var monkey in monkeys)
    {
        Console.WriteLine($"Monkey {monkey.Num}: {String.Join(", ", monkey.Items)}");
    }
}

foreach (int num in monkeyInspectionCount.Keys.OrderBy(n => n))
{
    int inspectionCount = monkeyInspectionCount[num];
    Console.WriteLine($"Monkey {num} inspected items {inspectionCount} times.");
}

int monkeyBusiness = monkeyInspectionCount.Values
    .OrderByDescending(n => n)
    .Take(2)
    .Aggregate(1, (acc, ic) => acc * ic);

Console.WriteLine(monkeyBusiness);

Func<int, int> newOperation(char op, string operatorValue)
    => operatorValue == "old"
    ? newOldOperation(op)
    : newIntOperation(op, int.Parse(operatorValue));

Func<int, int> newOldOperation(char op) => op switch
{
    '+' => (int n) => n + n,
    '*' => (int n) => n * n,
    _ => throw new Exception($"operator {op} unhandled"),
};

Func<int, int> newIntOperation(char op, int operatorValue) => op switch
{
    '+' => (int n) => n + operatorValue,
    '*' => (int n) => n * operatorValue,
    _ => throw new Exception($"operator {op} unhandled"),
};

Func<int, bool> newTestOperation(int divby) => n => n % divby == 0;

record Monkey(
    int Num,
    List<int> Items,
    Func<int, int> Operation,
    Func<int, bool> Test,
    int ThrowToIfTrue,
    int ThrowToIfFalse
);
