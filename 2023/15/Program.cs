const string file = "in.txt";

string text = File.ReadAllText(file);
string[] steps = text.Split(',');
int[] results = steps.Select(HASH).ToArray();
int sum = results.Sum();

Console.WriteLine(sum);

static int HASH(string s)
{
    int hash = 0;
    for (int i = 0; i < s.Length; i++)
    {
        int c = s[i]; // "Determine the ASCII code for the current character of the string."
        hash += c;    // "Increase the current value by the ASCII code you just determined."
        hash *= 17;   // "Set the current value to itself multiplied by 17."
        hash %= 256;  // "Set the current value to the remainder of dividing itself by 256."
    }
    return hash;
}
