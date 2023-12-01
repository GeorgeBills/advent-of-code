string file = args.Length >= 1 ? args[0] : "eg.txt";

int answer = File.ReadAllLines(file)
    .Select(line =>
        {
            var digits = line.ToCharArray().Where(c => char.IsDigit(c));
            int first = ToDigit(digits.First());
            int last = ToDigit(digits.Last());
            return 10 * first + last;
        }
    ).Sum();

Console.WriteLine($"{answer}");

static int ToDigit(char c) => c - '0';
