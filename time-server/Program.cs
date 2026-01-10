// See https://aka.ms/new-console-template for more information

using InDepth.Infrastructure.Timing;
using static System.Console;

TimeEntity server = new();
string Host1 = "DEVELOP1";
string Host2 = "DEVELOP2";

WriteLine("Staring Time Server...");
server.StartService();
WriteLine("Server started. Press any key to stop.");
do
{
    ConsoleKeyInfo key = ReadKey();
    if (key.Key == ConsoleKey.D1)
    {
        server.Connect(Host1, 5000);
        WriteLine(server.ToString());
    }
    else if (key.Key==ConsoleKey.D2)
    {
        server.Connect(Host2, 5000);
        WriteLine(server.ToString());
    }
    else break;
}
while (true);
server.StopService();




