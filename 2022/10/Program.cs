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

code = code[..codeidx];

/***********
 * EXECUTE *
 ***********/

const int rowWidth = 40;
const char litPixel = '#';
const char darkPixel = '.';

int x = 1; // "The CPU has a single register, X, which starts with the value 1."
var state = Expecting.Instruction;
for (int cycle = 1; cycle <= code.Length; cycle++)
{
    int rowX = (cycle - 1) % rowWidth;
    if (rowX == x - 1 || rowX == x || rowX == x + 1)
    {
        Console.Write(litPixel);
    }
    else
    {
        Console.Write(darkPixel);
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

    if (cycle % 40 == 0)
    {
        Console.WriteLine();
    }
}

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
