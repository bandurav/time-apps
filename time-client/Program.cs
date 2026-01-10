using InDepth.Infrastructure.Timing;
using System;
using static System.Console;

//const string Host = "0.pool.ntp.org";
const string Host = "DEVELOP2";
const int TimeOut = 5000;
WriteLine("Time Client v1.0");            
WriteLine("(C)2001-2019 Valer BOCAN, PhD <valer@bocan.ro>");
WriteLine();
WriteLine($"Connecting to {Host}...");
WriteLine();

try
{
    var client = new TimeEntity();
    client.Connect(Host, TimeOut);
    WriteLine(client.ToString());
}
catch (Exception ex)
{
    WriteLine($"Error: {ex.Message}");
}
