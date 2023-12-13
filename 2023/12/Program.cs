const string file = "eg.txt";

const char operationalch = '.';
const char damagedch = '#';
const char unknownch = '?';

var rows = File.ReadLines(file)
    .Select(l => l.Split(' '))
    .Select(s => (
        Springs: s[0].Split(operationalch, StringSplitOptions.RemoveEmptyEntries).ToArray(),
        Damaged: s[1].Split(',').Select(int.Parse).ToArray()
    ));

foreach (var (springs, damaged) in rows)
{
    X(springs, damaged);
}

void X(string[] strs, int[] nums)
{
#if DEBUG
    Console.WriteLine($"{nameof(X)} {string.Join(',', strs)} {string.Join(',', nums)}");
#endif

    // any str that contains one or more damaged springs must map to at least one num
    int unallocated = nums.Length;
    var allocations = new int[strs.Length];
    for (int i = 0; i < strs.Length; i++)
    {
        if (strs.ElementAt(i).Contains(damagedch))
        {
            allocations[i] = 1;
            unallocated--;
        }
    }

    Y(strs, nums, allocations, unallocated);
}

void Y(string[] strs, int[] nums, int[] allocations, int unallocated)
{
    var x = Enumerable.Zip(strs, allocations).Select(sa => $"{sa.First} >= {sa.Second}");

#if DEBUG
    Console.WriteLine($"{nameof(Y)}: {string.Join(',', x)} ({unallocated} unalloced)");
#endif

    // assess our allocation
    if (unallocated == 0)
    {
        int from = 0;
        for (int i = 0; i < allocations.Length; i++)
        {
            int len = allocations[i];
            if (len == 0)
            {
                continue;
            }
            int to = from + len;
            foreach (var str in Z(strs[i], nums[from..to]))
            {
                Console.WriteLine($"= {str}");
            }

            from += len;
        }
        return;
    }

    // try out all prospective allocations
    for (int i = 0; i < allocations.Length; i++)
    {
        allocations[i]++;
        Y(strs, nums, allocations, unallocated - 1);
        allocations[i]--;
    }
}

IEnumerable<string> Z(string str, int[] nums)
{
#if DEBUG
    Console.WriteLine($"{nameof(Z)}: {str} {string.Join(',', nums)}");
#endif

    if (!Possible(str, nums))
    {
        yield break;
    }

    int already = str.Count(c => c == damagedch);

    // try out all split points instead?
    // may only split on unknowns
    // then recurse back to Y?

    int n1 = nums[0];
    int dcidx = str.IndexOf(damagedch);
    for (int i = 0; i < str.Length - n1; i++)
    {
        if (i < dcidx && i + str.Length > dcidx)
        {
            // must connect with the first # if there is one and it's <= n1
        }

        string head = str[..i] + new string(damagedch, n1);

        if (nums.Length == 1)
        {
            // this is the proposal
            yield return str + str[(i + n1)..];
        }
        else
        {
            foreach (string tail in Z(str[(i + n1)..], nums[1..]))
            {
                yield return str + tail;
            }
        }
    }
}

static bool Possible(string str, int[] nums) => str.Length >= nums.Sum() + nums.Length - 1;
/*            ###         3           => 3             3            0
 *            #.#         1,1         => 3             2            2
 *            #.#.#.#     1,1,1,1     => 7             4            4
 *            #####.#     5,1         => 7             6            2 */
