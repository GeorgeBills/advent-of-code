using System.Text.RegularExpressions;

string file = args.Length >= 1 ? args[0] : "eg.txt";

var input = File.ReadAllLines(file);
long time = long.Parse(string.Concat(Regex.Matches(input[0], @"\d+").Select(m => m.Value)));
long distance = long.Parse(string.Concat(Regex.Matches(input[1], @"\d+").Select(m => m.Value)));
(long low, long high) = Solve(time, distance);
long ways = high - low + 1;
Console.WriteLine(ways);

(long low, long high) Solve(long time, long distance)
{
    //             acceleration * (time - acceleration) = distance
    //            -acceleration^2 + time * acceleration = distance
    // -acceleration^2 + time * acceleration - distance = 0
    double discriminant = Math.Pow(time, 2) - 4 * (distance + 1);
    double solution1 = (-1 * time + Math.Sqrt(discriminant)) / -2;
    double solution2 = (-1 * time - Math.Sqrt(discriminant)) / -2;
    return ((long)Math.Ceiling(solution1), (long)Math.Floor(solution2));
}
