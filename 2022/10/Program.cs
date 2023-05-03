using System.Text.RegularExpressions;

/*********
 * PARSE *
 *********/

const int initialSize = 8;

var addxRegex = new Regex(@"^addx (-?\d+)$", RegexOptions.Compiled);
var noopRegex = new Regex(@"^noop$", RegexOptions.Compiled);

int codeidx = 0;
byte[] code = new byte[initialSize];

foreach (var line in File.ReadLines(args[0]))
{
    Match match;

    match = addxRegex.Match(line);
    if (match.Success)
    {
        MaybeResize(ref code, codeidx + 2);
        code[codeidx++] = (byte)Instruction.AddX;
        code[codeidx++] = (byte)sbyte.Parse(match.Groups[1].Value);
        continue;
    }

    match = noopRegex.Match(line);
    if (match.Success)
    {
        MaybeResize(ref code, codeidx + 1);
        code[codeidx++] = (byte)Instruction.NoOp;
        continue;
    }

    throw new Exception($"unparsed line: {line}");

    void MaybeResize<T>(ref T[] array, int required)
    {
        const int growMultiplier = 2;
        if (required > code.Length)
        {
            Array.Resize(ref array, array.Length * growMultiplier);
        }
    }
}

Console.WriteLine($"parsed {codeidx} bytes into array of length {code.Length}");

code = code[..codeidx];

/***********
 * EXECUTE *
 ***********/

int x = 1; // "The CPU has a single register, X, which starts with the value 1."
int sum = 0;
var state = Expecting.Instruction;
for (int cycle = 1; cycle <= code.Length; cycle++)
{
    if ((cycle - 20) % 40 == 0)
    {
        int strength = cycle * x;
        sum += strength;
        Console.WriteLine($"{cycle}: x = {x}; strength = {strength}");
    }

    byte b = code[cycle - 1];
    switch (state)
    {
        case Expecting.Instruction:
            switch ((Instruction)b)
            {
                case Instruction.NoOp:
                    break;
                case Instruction.AddX:
                    state = Expecting.AddXOperand;
                    break;
                default:
                    throw new Exception($"unexpected instruction at index {cycle - 1}: {ByteToString(b)}");
            }
            break;
        case Expecting.AddXOperand:
            sbyte operand = (sbyte)b;
            x += operand;
            state = Expecting.Instruction;
            break;
    }
}

Console.WriteLine(sum);

string ByteToString(byte b) => "0b" + Convert.ToString(b, 2).PadLeft(8, '0');

enum Expecting
{
    Instruction,
    AddXOperand,
}

enum Instruction : byte
{
    NoOp = 0b0000_0000,
    AddX = 0b0000_0001,
}
