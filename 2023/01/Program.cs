string file = args.Length >= 1 ? args[0] : "eg.txt";

string[] digitstrs = new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

char[] digitchars = new[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' }; /* no 0's in my input ;) */

int answer = File.ReadAllLines(file)
    .Select(line =>
        {
            int first = GetFirst(line);
            int last = GetLast(line);
            int number = 10 * first + last;
            return number;
        }
    ).Sum();

Console.WriteLine(answer);

int GetFirst(string line)
{
    var strs = digitstrs.Select((str, idx) => (digit: idx + 1, index: line.IndexOf(str)));
    var chs = digitchars.Select((ch, idx) => (digit: idx + 1, index: line.IndexOf(ch)));
    return Enumerable.Concat(strs, chs)
        .Where(digitindex => digitindex.index != -1)
        .MinBy(digitindex => digitindex.index)
        .digit;
}

int GetLast(string line)
{
    var strs = digitstrs.Select((str, idx) => (digit: idx + 1, index: line.LastIndexOf(str)));
    var chs = digitchars.Select((ch, idx) => (digit: idx + 1, index: line.LastIndexOf(ch)));
    return Enumerable.Concat(strs, chs)
        .Where(digitindex => digitindex.index != -1)
        .MaxBy(digitindex => digitindex.index)
        .digit;
}
