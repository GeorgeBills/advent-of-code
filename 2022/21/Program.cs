using System.Text;
using System.Text.RegularExpressions;

const string humn = "humn";
const string root = "root";

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

// Second, you got the wrong monkey for the job starting with humn:. It isn't a
// monkey - it's you. Actually, you got the job wrong, too: you need to figure
// out what number you need to yell so that root's equality check passes. (The
// number that appears after humn: in your input is now irrelevant.)
dict[humn] = new Variable(humn);

foreach (var kv in dict)
{
    if (kv.Value is ArithmeticOperation operation)
    {
        operation.Left = new ExpressionRef<long>(dict[operation.Left.ID]);
        operation.Right = new ExpressionRef<long>(dict[operation.Right.ID]);
    }
}

// "First, you got the wrong job for the monkey named root; specifically, you
// got the wrong math operation. The correct operation for monkey root should be
// =, which means that it still listens for two numbers (from the same two
// monkeys as before), but now checks that the two numbers match."
var rootArithmetic = (ArithmeticOperation)dict[root];
var rootEquality = (Expression<bool>)new EqualityOperation(root, rootArithmetic.Left, rootArithmetic.Right);

#if DEBUG
PrintTree(rootEquality);
#endif

string equationOriginal = rootEquality.ToEquationString();
Console.WriteLine($"equation (original): {equationOriginal}");

rootEquality = rootEquality.Simplify();

string equationSimplified = rootEquality.ToEquationString();
Console.WriteLine($"equation (simplified): {equationSimplified}");

void PrintTree<T>(Expression<T> expr, int depth = 0) // FIXME: PrintTree() is broken, put it on the interface
{
    Console.WriteLine(new string(' ', depth * 2) + expr);

    if (expr is ArithmeticOperation operation)
    {
        PrintTree(operation.Left.Expression, depth + 1);
        PrintTree(operation.Right.Expression, depth + 1);
    }
}

Expression<long> ParseLine(string line)
{
    Match match;

    match = operationRegex.Match(line);
    if (match.Success)
    {
        string id = match.Groups["id"].Value;
        var left = new ExpressionRef<long>(match.Groups["leftid"].Value);
        var op = match.Groups["op"].Value switch
        {
            "+" => Op.Plus,
            "-" => Op.Minus,
            "*" => Op.Multiply,
            "/" => Op.Divide,
        };
        var right = new ExpressionRef<long>(match.Groups["rightid"].Value);
        return new ArithmeticOperation(id, op, left, right);
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

interface Expression<T>
{
    string ID { get; }
    T Evaluate();
    string ToEquationString();
    Expression<T> Simplify();
}

class ExpressionRef<T>(string id, Expression<T>? expr = null) : Expression<T>
{
    public string ID => id;
    public Expression<T> Expression => expr;

    public ExpressionRef(Expression<T> e) : this(e.ID, e) { }

    public override string ToString() => id;

    public string ToEquationString() => expr != null
        ? expr.ToEquationString()
        : throw new Exception($"expression ref {id} has not been reified");

    public T Evaluate() => expr != null
        ? expr.Evaluate()
        : throw new Exception($"expression ref {id} has not been reified");

    public Expression<T> Simplify() => expr != null
        ? expr.Simplify()
        : throw new Exception($"expression ref {id} has not been reified");
}

class EqualityOperation(string id, ExpressionRef<long> left, ExpressionRef<long> right) : Expression<bool>
{
    public string ID => id;

    public override string ToString() => $"{id}: {left.ToString()} = {right.ToString()}";

    public bool Evaluate() => left.Evaluate() == right.Evaluate();

    public Expression<bool> Simplify() => new EqualityOperation(
        id,
        new ExpressionRef<long>(left.Simplify()),
        new ExpressionRef<long>(right.Simplify())
    );

    public string ToEquationString()
    {
        var sb = new StringBuilder();
        sb.Append('(');
        sb.Append(left.ToEquationString());
        sb.Append('=');
        sb.Append(right.ToEquationString());
        sb.Append(')');
        return sb.ToString();
    }
}

class ArithmeticOperation(string id, Op op, ExpressionRef<long> left, ExpressionRef<long> right) : Expression<long>
{
    public string ID => id;

    public override string ToString() => $"{id}: {left.ToString()} {(char)op} {right.ToString()}";

    public string ToEquationString()
    {
        var sb = new StringBuilder();
        sb.Append('(');
        sb.Append(left.ToEquationString());
        sb.Append((char)op);
        sb.Append(right.ToEquationString());
        sb.Append(')');
        return sb.ToString();
    }

    public ExpressionRef<long> Left { get => left; set => left = value; }
    public ExpressionRef<long> Right { get => right; set => right = value; }

    private long evaluate(long leftVal, long rightVal) => op switch
    {
        Op.Plus => leftVal + rightVal,
        Op.Minus => leftVal - rightVal,
        Op.Multiply => leftVal * rightVal,
        Op.Divide => leftVal / rightVal,
    };

    public long Evaluate() => evaluate(left.Evaluate(), right.Evaluate());

    public ArithmeticOperation WithLeft(ExpressionRef<long> newLeft) => new(id, op, newLeft, right);
    public ArithmeticOperation WithRight(ExpressionRef<long> newRight) => new(id, op, left, newRight);

    public Expression<long> Simplify() => (op, left.Simplify(), right.Simplify()) switch
    {
        (_, Number x, Number y) => new Number(null, evaluate(x.Num, y.Num)),
        (_, Number x, _) => this.WithLeft(new ExpressionRef<long>("", x)),
        (_, _, Number y) => this.WithRight(new ExpressionRef<long>("", y)),
        _ => this,
    };
}

class Number(string id, long num) : Expression<long>
{
    public string ID => id;
    public long Num => num;
    public override string ToString() => $"{id}: {num}";
    public string ToEquationString() => num.ToString();
    public long Evaluate() => num;
    public Expression<long> Simplify() => this;
}

class Variable(string id) : Expression<long>
{
    public string ID => id;
    public override string ToString() => id;
    public long Evaluate() => throw new NotImplementedException("can't evaluate a variable"); // LSP ickiness
    public Expression<long> Simplify() => this;
    public string ToEquationString() => id;
}

enum Op { Multiply = '*', Divide = '/', Plus = '+', Minus = '-' };
