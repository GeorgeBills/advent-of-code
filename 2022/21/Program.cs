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
var rootEquality = new EqualityOperation(root, rootArithmetic.Left, rootArithmetic.Right);

string equationOriginal = rootEquality.ToEquationString();
Console.WriteLine($"equation (original): {equationOriginal}");

#if DEBUG
Console.Write(rootEquality.ToTreeString());
#endif

rootEquality = (EqualityOperation)rootEquality.Simplify();

string equationSimplified = rootEquality.ToEquationString();
Console.WriteLine($"equation (simplified): {equationSimplified}");

#if DEBUG
Console.Write(rootEquality.ToTreeString());
#endif

rootEquality = rootEquality.Balance();

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
    string ToTreeString(int depth = 0);
    Expression<T> Simplify();
    int NodeCount();
}

class ExpressionRef<T>(string id, Expression<T>? expr = null) : Expression<T>
{
    public string ID => id;
    public Expression<T> Expression => expr;

    public ExpressionRef(Expression<T> e) : this(e.ID, e) { }

    public override string ToString() => id;

    private U ifexpr<U>(Func<Expression<T>, U> func) => expr != null
        ? func(expr)
        : throw new Exception($"expression ref {id} has not been reified");

    public string ToEquationString() => ifexpr(expr => expr.ToEquationString());

    public string ToTreeString(int depth) => ifexpr(expr => expr.ToTreeString(depth));

    public T Evaluate() => ifexpr(expr => expr.Evaluate());

    public Expression<T> Simplify() => ifexpr(expr => expr.Simplify());

    public int NodeCount() => ifexpr(expr => expr.NodeCount());
}

class EqualityOperation(string id, ExpressionRef<long> left, ExpressionRef<long> right) : Expression<bool>
{
    public string ID => id;
    public ExpressionRef<long> Left => left;
    public ExpressionRef<long> Right => right;

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

    public string ToTreeString(int depth = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string(' ', depth * 2) + '=');
        sb.Append(left.Expression.ToTreeString(depth + 1));
        sb.Append(right.Expression.ToTreeString(depth + 1));
        return sb.ToString();
    }

    public EqualityOperation Balance()
    {
        var current = this;
        while (current.Left.NodeCount() > 1 || current.Right.NodeCount() > 1)
        {
            current = current.BalanceSingle();
#if DEBUG
            Console.WriteLine(current.ToEquationString());
#endif
        }
        return current;
    }

    public EqualityOperation BalanceSingle() => (left.Expression, right.Expression) switch
    {
        (ArithmeticOperation left, Number right) => balanceSingle(left, right),
        // (Number n, ArithmeticOperation ao) => balance(ao, n).Swap(),
    };

    private EqualityOperation WithLeft(Expression<long> newLeft) => WithLeft(new ExpressionRef<long>(newLeft));
    private EqualityOperation WithLeft(ExpressionRef<long> newLeft) => new("", newLeft, right);
    private EqualityOperation WithRight(Expression<long> newRight) => WithRight(new ExpressionRef<long>(newRight));
    private EqualityOperation WithRight(ExpressionRef<long> newRight) => new("", left, newRight);

    private EqualityOperation balanceSingle(ArithmeticOperation left, Number right) => (left.Op, left.Left.Expression, left.Right.Expression) switch
    {
        (Op.Plus, Expression<long> sub, Number addend) => this.WithLeft(sub).WithRight(right.Minus(addend)),
        (Op.Plus, Number addend, Expression<long> sub) => this.WithLeft(sub).WithRight(right.Minus(addend)),
        (Op.Divide, Expression<long> sub, Number divisor) => this.WithLeft(sub).WithRight(right.Multiply(divisor)),
        (Op.Minus, Expression<long> sub, Number subtrahend) => this.WithLeft(sub).WithRight(right.Plus(subtrahend)),
        (Op.Minus, Number minuend, Expression<long> sub) => this.WithLeft(sub).WithRight(right.Plus(minuend).Negate()),
        (Op.Multiply, Expression<long> sub, Number multiplier) => this.WithLeft(sub).WithRight(right.Divide(multiplier)),
        (Op.Multiply, Number multiplier, Expression<long> sub) => this.WithLeft(sub).WithRight(right.Divide(multiplier)),
    };

    public int NodeCount() => 1 + left.NodeCount() + right.NodeCount();
}

class ArithmeticOperation(string id, Op op, ExpressionRef<long> left, ExpressionRef<long> right) : Expression<long>
{
    public string ID => id;
    public Op Op => op;

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

    public string ToTreeString(int depth)
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string(' ', depth * 2) + (char)op);
        sb.Append(left.Expression.ToTreeString(depth + 1));
        sb.Append(right.Expression.ToTreeString(depth + 1));
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

    public ArithmeticOperation WithOp(Op newOp) => new(id, newOp, left, right);
    public ArithmeticOperation WithLeft(ExpressionRef<long> newLeft) => new(id, op, newLeft, right);
    public ArithmeticOperation WithRight(ExpressionRef<long> newRight) => new(id, op, left, newRight);
    public ArithmeticOperation WithLeft(Expression<long> newLeft) => WithLeft(new ExpressionRef<long>(newLeft));
    public ArithmeticOperation WithRight(Expression<long> newRight) => WithRight(new ExpressionRef<long>(newRight));

    public Expression<long> Simplify() => (op, left.Simplify(), right.Simplify()) switch
    {
        (Op op, Number x, Number y) => new Number(left.ID + (char)op + right.ID, evaluate(x.Num, y.Num)),
        (Op.Multiply, Number n, ArithmeticOperation e) => e.Multiply(n),
        (Op.Multiply, ArithmeticOperation e, Number n) => e.Multiply(n),
        (Op.Plus, Number n, ArithmeticOperation e) when e.Op == Op.Plus || e.Op == Op.Minus => e.Plus(n.Num),
        (Op.Plus, ArithmeticOperation e, Number n) when e.Op == Op.Plus || e.Op == Op.Minus => e.Plus(n.Num),
        (Op.Minus, Number n, ArithmeticOperation e) when e.Op == Op.Plus || e.Op == Op.Minus => e.Plus(-1 * n.Num),
        (Op.Minus, ArithmeticOperation e, Number n) when e.Op == Op.Plus || e.Op == Op.Minus => e.Plus(-1 * n.Num),
        (_, var x, var y) => this.WithLeft(x).WithRight(y),
    };

    public ArithmeticOperation Plus(long addend) => (
        (op, left.Expression, right.Expression) switch
        {
            (Op.Plus, Number n, _) => this.WithLeft(new Number("", addend + n.Num)),          // (n + x) + a => (a + n) + x
            (Op.Plus, _, Number n) => this.WithRight(new Number("", addend + n.Num)),         // (x + n) + a => x + (a + n)
            (Op.Minus, Number n, _) => this.WithLeft(new Number("", addend + n.Num)),         // (n - x) + a => (a + n) - x
            (Op.Minus, _, Number n) => this.WithRight(new Number("", -1 * (addend - n.Num))), // (x - n) + a => x + (a - n)
        }
    ).Canonicalise();

    public ArithmeticOperation Canonicalise() => (op, left.Expression, right.Expression) switch
    {
        (Op.Plus, _, Number n) when n.Num < 0 => this.WithOp(Op.Minus).WithRight(n.Negate()),
        (Op.Minus, _, Number n) when n.Num < 0 => this.WithOp(Op.Plus).WithRight(n.Negate()),
        _ => this,
    };

    public ArithmeticOperation Multiply(Number multiplier)
    {
        var l = multiply(left.Expression, multiplier);
        var r = multiply(right.Expression, multiplier);
        return new ArithmeticOperation(
            multiplier.ID + (char)Op.Multiply + this.ID,
            op,
            new ExpressionRef<long>(l),
            new ExpressionRef<long>(r)
        );

        // TODO: should be able to clean this type switch up with better typing...
        static Expression<long> multiply(Expression<long> expr, Number multiplier) => expr switch
        {
            Number num => num.Multiply(multiplier),
            Variable var => var.Multiply(multiplier),
            _ => expr,
        };
    }

    public int NodeCount() => 1 + left.NodeCount() + right.NodeCount();
}

class Number(string id, long num) : Expression<long>
{
    public string ID => id;
    public long Num => num;
    public override string ToString() => $"{id}: {num}";
    public string ToEquationString() => num.ToString();
    public long Evaluate() => num;
    public Expression<long> Simplify() => this;
    public string ToTreeString(int depth) => new string(' ', depth * 2) + num + "\n";

    public Number Negate() => new(id, -1 * num);

    public Number Multiply(Number multiplier) => new Number(
        multiplier.ID + (char)Op.Multiply + this.ID,
        multiplier.Num * this.Num
    );

    public Number Minus(Number subtrahend) => new Number("", this.Num - subtrahend.Num);

    public Number Plus(Number addend) => new Number("", this.Num + addend.Num);

    public Number Divide(Number divisor) => new Number("", this.Num / divisor.Num); // TODO: worry about integer math here?

    public int NodeCount() => 1;
}

class Variable(string id) : Expression<long>
{
    public string ID => id;
    public override string ToString() => id;
    public long Evaluate() => throw new NotImplementedException("can't evaluate a variable"); // LSP ickiness
    public Expression<long> Simplify() => this;
    public string ToEquationString() => id;
    public string ToTreeString(int depth) => new string(' ', depth * 2) + id + "\n";

    public Expression<long> Multiply(Number multiplier) => new ArithmeticOperation(
        multiplier.ID + (char)Op.Multiply + this.ID,
        Op.Multiply,
        new ExpressionRef<long>(multiplier),
        new ExpressionRef<long>(this)
    );

    public int NodeCount() => 1;
}

enum Op { Multiply = '*', Divide = '/', Plus = '+', Minus = '-' };
