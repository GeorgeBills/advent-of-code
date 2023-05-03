var lines = File.ReadLines("in.txt");

var elves = new List<int>() { 0 };
foreach (string line in lines)
{
    if (line == "")
    {
        elves.Add(0); // new elf
        continue;
    }

    int calories = int.Parse(line);
    elves[^1] += calories;
}

int sum = elves
    .OrderByDescending(n => n) // small enough that sorting is fine
    .Take(3)
    .Sum();

Console.WriteLine(sum);
