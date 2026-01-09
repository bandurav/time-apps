// See https://aka.ms/new-console-template for more information

using InDepth.Infrastructure.Timing;
using static System.Console;

TimeEntity server = new();

WriteLine("Staring Time Server...");
server.StartService();
WriteLine("Server started. Press any key to stop.");
ReadKey();
server.StopService();




