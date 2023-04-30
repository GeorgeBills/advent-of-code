var rbStartPacket = new char[4];

var rbStartMessage = new char[14];

string line = File.ReadAllText(args[0]);

bool foundPacket = false, foundMessage = false;
for (int i = 0; i < line.Length; i++)
{
    rbStartPacket[i % rbStartPacket.Length] = line[i];
    if (!foundPacket && i >= rbStartPacket.Length && rbStartPacket.Distinct().Count() == rbStartPacket.Length)
    {
        Console.WriteLine($"packet: {i + 1}");
        foundPacket = true;
    }

    rbStartMessage[i % rbStartMessage.Length] = line[i];
    if (!foundMessage && i >= rbStartMessage.Length && rbStartMessage.Distinct().Count() == rbStartMessage.Length)
    {
        Console.WriteLine($"message: {i + 1}");
        foundMessage = true;
    }

    if (foundPacket && foundMessage)
    {
        break;
    }
}
