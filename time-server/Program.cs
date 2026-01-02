// See https://aka.ms/new-console-template for more information

using SntpComponents;
using static System.Console;

SNTPServer server = new();

WriteLine("Staring SNTP Server...");
server.StartService();
WriteLine("Server started. Press any key to stop.");
ReadKey();
server.StopService();




