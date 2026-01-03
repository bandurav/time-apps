// See https://aka.ms/new-console-template for more information

using InDepth.Time;
using static System.Console;

SNTPEntity server = new();

WriteLine("Staring SNTP Server...");
server.StartService();
WriteLine("Server started. Press any key to stop.");
ReadKey();
server.StopService();




