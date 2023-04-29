using System.Text.RegularExpressions;

var assignmentRegex = new Regex(@"^(\d+)-(\d+),(\d+)-(\d+)$");

var fullyContains = ((int start, int end) a, (int start, int end) b) =>
    a.start >= b.start && a.end <= b.end || // b fully contains a
    b.start >= a.start && b.end <= a.end;   // a fully contains b

var overlaps = ((int start, int end) a, (int start, int end) b) =>
    a.start <= b.end && a.end >= b.start ||
    b.start <= a.end && b.end >= a.start;

var assignments = File.ReadLines(args[0])
    .Select(line => assignmentRegex.Match(line))
    .Select(match => match.Groups)
    .Select(groups => new[] {
        (start: int.Parse(groups[1].Value), end: int.Parse(groups[2].Value)),
        (start: int.Parse(groups[3].Value), end: int.Parse(groups[4].Value)),
    });

int answer1 = assignments.Count(assignmentPair => fullyContains(assignmentPair[0], assignmentPair[1]));
Console.WriteLine($"p1: {answer1}");

int answer2 = assignments.Count(assignmentPair => overlaps(assignmentPair[0], assignmentPair[1]));
Console.WriteLine($"p2: {answer2}");
