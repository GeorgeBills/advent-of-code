using System.Text;
using System.Text.RegularExpressions;

var cdRegex = new Regex(@"^\$ cd (/|\.\.|\w+)$");
var lsRegex = new Regex(@"^\$ ls$");
var dirRegex = new Regex(@"^dir (\w+)$");
var fileRegex = new Regex(@"^(\d+) ([\w\.]+)$");

var parseLine = ITerminalOutput (string line) =>
{
    Match match;

    match = cdRegex.Match(line);
    if (match.Success)
    {
        return new ChangeDirectoryCommand(dirname: match.Groups[1].Value);
    }

    match = lsRegex.Match(line);
    if (match.Success)
    {
        return new ListCommand();
    }

    match = dirRegex.Match(line);
    if (match.Success)
    {
        return new DirInfo(dirname: match.Groups[1].Value);
    }

    match = fileRegex.Match(line);
    if (match.Success)
    {
        return new FileInfo(size: int.Parse(match.Groups[1].Value), filename: match.Groups[2].Value);
    }

    throw new Exception($"could not parse line: {line}");
};

var output = File.ReadLines(args[0]).Select(line => parseLine(line));

var first = output.First();
if (first is not ChangeDirectoryCommand { dirname: "/" })
{
    throw new Exception("expected to cd to root directory as the first command");
}

var root = new DirNode("/", null);
var current = root;

foreach (var o in output.Skip(1))
{
    current = o.Apply(current);
}

Console.Write(root.ToString());
Console.WriteLine();

const int maxSize = 100_000;

int answer = root.Flatten().Select(dn => dn.TotalSize()).Where(sz => sz <= maxSize).Sum();
Console.WriteLine(answer);



interface ITerminalOutput
{
    DirNode Apply(DirNode current);
}

record ChangeDirectoryCommand(string dirname) : ITerminalOutput
{
    public DirNode Apply(DirNode current) => current.GetDir(dirname);
}

record ListCommand() : ITerminalOutput
{
    public DirNode Apply(DirNode current) => current; // no op
}

record DirInfo(string dirname) : ITerminalOutput
{
    public DirNode Apply(DirNode current) => current.AddDir(dirname);
}

record FileInfo(int size, string filename) : ITerminalOutput
{
    public DirNode Apply(DirNode current) => current.AddFile(filename, size);
}

class DirNode
{
    public string Name { get; init; }
    private DirNode? parent;
    private readonly Dictionary<string, DirNode> dirChildren;
    private readonly Dictionary<string, int> fileChildren;

    public DirNode(string name, DirNode? parent)
    {
        this.Name = name;
        this.parent = parent;
        this.dirChildren = new Dictionary<string, DirNode>();
        this.fileChildren = new Dictionary<string, int>();
    }

    public DirNode GetDir(string dirname)
    {
        if (dirname == "..")
        {
            if (parent == null)
            {
                throw new Exception("attempt to get parent for node with no parents");
            }
            return parent;
        }

        return dirChildren[dirname];
    }

    public DirNode AddDir(string dirname)
    {
        dirChildren.TryAdd(dirname, new DirNode(dirname, this));
        return this;
    }

    public DirNode AddFile(string filename, int size)
    {
        fileChildren[filename] = size;
        return this;
    }

    public int TotalSize() => fileChildren.Values.Sum() + dirChildren.Values.Select(dc => dc.TotalSize()).Sum();

    public IEnumerable<DirNode> Flatten()
    {
        yield return this;
        foreach (var dc in dirChildren.Values)
        {
            foreach (var dn in dc.Flatten())
            {
                yield return dn;
            }
        }
    }

    public override string ToString() => ToString(indent: 0);

    public string ToString(int indent = 0)
    {
        var sb = new StringBuilder();
        sb.Append(new String(' ', indent * 2));
        sb.AppendLine($"- {Name} (dir)");

        indent++;

        foreach (var dc in dirChildren)
        {
            sb.Append(dc.Value.ToString(indent));
        }

        string indentation = new String(' ', indent * 2);
        foreach (var fc in fileChildren)
        {
            sb.Append(indentation);
            sb.AppendLine($"- {fc.Key} (file, size={fc.Value})");
        }

        return sb.ToString();
    }
};
