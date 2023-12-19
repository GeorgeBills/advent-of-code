
using System.Text;
using System.Text.RegularExpressions;

const string file = "in.txt";

string text = File.ReadAllText(file);
var steps = text.Split(',').Select(ParseStep);
var boxes = HASHMAP(steps);
int focus = CalculateFocus(boxes);
Console.WriteLine(focus);

static LinkedListNode<T>? FindLinkedListNodeWhere<T>(LinkedList<T> list, Func<T, bool> where)
{
    var current = list.First;
    while (current != null)
    {
        if (where(current.Value))
        {
            return current;
        }
        current = current.Next;
    }
    return null;
}

#if DEBUG
static void PrintBoxes(LinkedList<(string, int)>[] boxes)
{
    for (int i = 0; i <= byte.MaxValue; i++)
    {
        if (boxes[i].Count > 0)
        {
            Console.WriteLine($"Box {i}: {string.Join(' ', boxes[i])}");
        }
    }
}
#endif

static byte HASH(string s)
{
    byte hash = 0;
    foreach (var b in Encoding.UTF8.GetBytes(s))
    {
        hash += b;
        hash *= 17;
    }
    return hash;
}

static (string Label, char Operation, int FocalLength) ParseStep(string step)
{
    const string pattern = @"^([a-z]+)([-=])(\d)?$";
    var match = Regex.Match(step, pattern);
    if (!match.Success)
    {
        throw new ArgumentException(nameof(step), $"invalid step: {step}");
    }
    return (
        match.Groups[1].Value,
        char.Parse(match.Groups[2].Value),
        match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0
    );
}

static int CalculateFocus(LinkedList<(string Label, int FocalLength)>[] boxes)
{
    int focus = 0;
    for (int i = 0; i <= byte.MaxValue; i++)
    {
        int slot = 1;
        foreach (var (label, focal) in boxes[i])
        {
            int f = (i + 1) * slot * focal;
#if DEBUG
            Console.WriteLine($"{label}: {i + 1} * {slot} * {focal} = {f}");
#endif
            focus += f;
            slot++;
        }
    }

    return focus;
}

static LinkedList<(string Label, int FocalLength)>[] HASHMAP(IEnumerable<(string Label, char Operation, int FocalLength)> steps)
{
    var boxes = new LinkedList<(string Label, int FocalLength)>[byte.MaxValue + 1];
    for (int i = 0; i <= byte.MaxValue; i++)
    {
        boxes[i] = new LinkedList<(string, int)>();
    }

    foreach (var (label, operation, focal) in steps)
    {
        byte hash = HASH(label);
        var existing = FindLinkedListNodeWhere(boxes[hash], v => v.Label == label);
        switch (operation)
        {
            case '-':
                if (existing != null)
                {
                    boxes[hash].Remove(existing);
                }
                break;
            case '=':
                var node = new LinkedListNode<(string, int)>((label, focal));
                if (existing != null)
                {
                    boxes[hash].AddAfter(existing, node);
                    boxes[hash].Remove(existing);
                }
                else
                {
                    boxes[hash].AddLast(node);
                }
                break;
        }

#if DEBUG
        Console.WriteLine($"After {label}{operation}{focal}:");
        PrintBoxes(boxes);
#endif
    }

    return boxes;
}
