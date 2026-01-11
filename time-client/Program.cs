using InDepth.Infrastructure.Timing;
using System;
using static System.Console;

//const string Host = "0.pool.ntp.org";
string Host1 = "DEVELOP1";
string Host2 = "DEVELOP2";
const int TimeOut = 5000;
WriteLine("Time Client v1.0");            
WriteLine("(C)2001-2019 Valer BOCAN, PhD <valer@bocan.ro>");
WriteLine();
WriteLine($"Press 1 to connect to {Host1} server");
WriteLine($"Press 2 to connect to {Host2} server");
WriteLine("Press ESC key to brak");
var client = new TimeEntity();

do
{
    try
    {
        ConsoleKeyInfo key = ReadKey();
        if (key.Key == ConsoleKey.D1)
        {
            client.Connect(Host1, TimeOut);
        }
        else if (key.Key == ConsoleKey.D2)
        {
            client.Connect(Host2, TimeOut);
        }
        WriteLine(client.ToString());
        if (key.Key == ConsoleKey.Escape) break;
    }
    catch (Exception ex)
    {
        WriteLine($"Error: {ex.Message}");
    }
}
while (true);
