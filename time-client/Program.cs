using InDepth.Time;
using System;
using static System.Console;

//const string Host = "0.pool.ntp.org";
const string Host = "WIN-ARAF8S50IK1";
const int TimeOut = 5000;
WriteLine("SNTP Client v1.0");            
WriteLine("(C)2001-2019 Valer BOCAN, PhD <valer@bocan.ro>");
WriteLine();
WriteLine($"Connecting to {Host}...");
WriteLine();

try
{
    var client = new SNTPEntity();
    client.Connect(Host, TimeOut);
    Console.WriteLine(client.ToString());
}
catch (Exception ex)
{
    WriteLine($"Error: {ex.Message}");
}
