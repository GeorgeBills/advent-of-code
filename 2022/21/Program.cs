using System.Text.RegularExpressions;

string file = args.Length == 1 ? args[0] : "eg.txt";

var operationRegex = new Regex(@"^(?<id>\w+): (?<leftid>\w+) (?<op>[+*/-]) (?<rightid>\w+)$");
var numberRegex = new Regex(@"^(?<id>\w+): (?<num>\d+)$");

var parsed = File.ReadLines(file).Select(ParseLine);

#if DEBUG
foreach (var e in parsed)
{
    Console.WriteLine(e);
}
#endif

var dict = parsed.ToDictionary(p => p.ID, p => p);

foreach (var kv in dict)
{
    if (kv.Value is Operation operation)
    {
        operation.Left = new ExpressionRef(dict[operation.Left.ID]);
        operation.Right = new ExpressionRef(dict[operation.Right.ID]);
    }
}

var root = dict["root"];

long answer = root.Evaluate();

Console.WriteLine($"the answer is {answer}");

Expression ParseLine(string line)
{
    Match match;

    match = operationRegex.Match(line);
    if (match.Success)
    {
        string id = match.Groups["id"].Value;
        var left = new ExpressionRef(match.Groups["leftid"].Value);
        var op = match.Groups["op"].Value switch
        {
            "+" => Op.Plus,
            "-" => Op.Minus,
            "*" => Op.Multiply,
            "/" => Op.Divide,
        };
        var right = new ExpressionRef(match.Groups["rightid"].Value);
        return new Operation(id, op, left, right);
    }

    match = numberRegex.Match(line);
    if (match.Success)
    {
        string id = match.Groups["id"].Value;
        int num = int.Parse(match.Groups["num"].Value);
        return new Number(id, num);
    }

    throw new Exception($"couldn't match line: {line}");
}

interface Expression
{
    string ID { get; }
    long Evaluate();
}

class ExpressionRef(string id, Expression? expr = null) : Expression
{
    public string ID => id;
    public override string ToString() => id;
    public ExpressionRef(Expression e) : this(e.ID, e) { }
    public long Evaluate() => expr != null
        ? expr.Evaluate()
        : throw new Exception($"expression ref {id} has not been reified");
}

class Operation(string id, Op op, ExpressionRef left, ExpressionRef right) : Expression
{
    public string ID => id;
    public override string ToString() => $"{id}: {left.ToString()} {op} {right.ToString()}";
    public ExpressionRef Left { get => left; set => left = value; }
    public ExpressionRef Right { get => right; set => right = value; }
    public long Evaluate() => op switch
    {
        Op.Plus => left.Evaluate() + right.Evaluate(),
        Op.Minus => left.Evaluate() - right.Evaluate(),
        Op.Multiply => left.Evaluate() * right.Evaluate(),
        Op.Divide => left.Evaluate() / right.Evaluate(),
    };
}

class Number(string id, int num) : Expression
{
    public string ID => id;
    public override string ToString() => $"{id}: {num}";
    public long Evaluate() => num;
}

enum Op { Multiply = '*', Divide = '/', Plus = '+', Minus = '-' };
