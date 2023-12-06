using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

var input = File.ReadAllLines(file);
var times = Regex.Matches(input[0], @"\d+").Select(m => m.Value).Select(int.Parse);
var distances = Regex.Matches(input[1], @"\d+").Select(m => m.Value).Select(int.Parse);
var races = Enumerable.Zip(times, distances);
var solutions = races.Select(Solve).ToArray();
var ways = solutions.Select(s => s.high - s.low + 1);

Console.WriteLine($"{string.Join(" x ", ways)} = {ways.Aggregate((a, b) => a * b)}");

(int low, int high) Solve((int, int) race)
{
    (int time, int distance) = race;
    //             acceleration * (time - acceleration) = distance
    //            -acceleration^2 + time * acceleration = distance
    // -acceleration^2 + time * acceleration - distance = 0
    double discriminant = Math.Pow(time, 2) - 4 * (distance + 1);
    double solution1 = (-1 * time + Math.Sqrt(discriminant)) / -2;
    double solution2 = (-1 * time - Math.Sqrt(discriminant)) / -2;
    return ((int)Math.Ceiling(solution1), (int)Math.Floor(solution2));
}
