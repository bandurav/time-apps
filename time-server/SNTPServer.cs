using ro.bocan.sntpclient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SntpComponents;

public class SNTPServer
{
    #region Private stuff
    // SNTP Data Structure Length
    private const byte SNTPDataLength = 48;

    // SNTP Data Structure (as described in RFC 2030)
    private readonly byte[] SNTPData = new byte[SNTPDataLength];

    // Offset constants for timestamps in the data structure
    private const byte offReferenceID = 12;
    private const byte offReferenceTimestamp = 16;
    private const byte offOriginateTimestamp = 24;
    private const byte offReceiveTimestamp = 32;
    private const byte offTransmitTimestamp = 40;
    private CancellationTokenSource cts = new CancellationTokenSource();
    #endregion
    #region Public accessors
    /// <summary>
    /// Warns of an impending leap second to be inserted/deleted in the last
    /// minute of the current day. (See the _LeapIndicator enum)
    /// </summary>
    public LeapIndicator LeapIndicator
    {
        // Isolate the two most significant bits
        get
        {
            byte val = (byte)(SNTPData[0] >> 6);
            switch (val)
            {
                case 0: return LeapIndicator.NoWarning;
                case 1: return LeapIndicator.LastMinute61;
                case 2: return LeapIndicator.LastMinute59;
                case 3: goto default;
                default:
                    return LeapIndicator.Alarm;
            }
        }
        set
        {
            switch (value)
            {
                case LeapIndicator.NoWarning: 
                    SNTPData[0] = (byte)(SNTPData[0] & 0x3F);
                    break;
                case LeapIndicator.LastMinute61:
                    SNTPData[0] = (byte)((SNTPData[0] & 0x3F) | 0x40);
                    break;
                case LeapIndicator.LastMinute59:
                    SNTPData[0] = (byte)((SNTPData[0] & 0x3F) | 0x80);
                    break;
                case LeapIndicator.Alarm:
                default:
                    SNTPData[0] = (byte)((SNTPData[0] & 0x3F) | 0xC0);
                    break;
            }
            byte val = (byte)(SNTPData[0] >> 6);

        }
    }

    /// <summary>
    /// Version number of the protocol (3 or 4).
    /// </summary>
    public byte VersionNumber
    {
        // Isolate bits 3 - 5
        get
        {
            byte val = (byte)((SNTPData[0] & 0x38) >> 3);
            return val;
        }
        set
        {
            byte val = (byte)(value << 3);
            SNTPData[0] = (byte)((SNTPData[0] & 0xC7) | val);
        }
    }

    /// <summary>
    /// Returns mode. (See the _Mode enum)
    /// </summary>
    public Mode Mode
    {
        // Isolate bits 0 - 3
        get
        {
            byte val = (byte)(SNTPData[0] & 0x7);
            switch (val)
            {
                case 0: goto default;
                case 6: goto default;
                case 7: goto default;
                default:
                    return Mode.Unknown;
                case 1:
                    return Mode.SymmetricActive;
                case 2:
                    return Mode.SymmetricPassive;
                case 3:
                    return Mode.Client;
                case 4:
                    return Mode.Server;
                case 5:
                    return Mode.Broadcast;
            }
        }
        set
        {
           byte val;
           switch (value)
            {
                case Mode.SymmetricActive:
                    val = 1;
                    break;
                case Mode.SymmetricPassive:
                    val = 2;
                    break;
                case Mode.Client:
                    val = 3;
                    break;
                case Mode.Server:
                    val = 4;
                    break;
                case Mode.Broadcast:
                    val = 5;
                    break;
                default:
                    val = 0;
                    break;
            }
            SNTPData[0] = (byte)((SNTPData[0] & 0xF8) | val);
        }
    }

    /// <summary>
    /// Stratum of the clock. (See the _Stratum enum)
    /// </summary>
    public Stratum Stratum
    {
        get
        {
            byte val = (byte)SNTPData[1];
            if (val == 0) return Stratum.Unspecified;
            else
                if (val == 1) return Stratum.PrimaryReference;
            else
                    if (val <= 15) return Stratum.SecondaryReference;
            else
                return Stratum.Reserved;
        }
        set
        {
            switch (value)
            {
                case Stratum.Unspecified:
                    SNTPData[1] = 0;
                    break;
                case Stratum.PrimaryReference:
                    SNTPData[1] = 1;
                    break;
                case Stratum.SecondaryReference:
                    SNTPData[1] = 2;
                    break;
                default:
                    SNTPData[1] = 0x10;
                    break;
            }
        }
    }
    /// <summary>
    /// Poll interval byte raw value
    /// </summary>
    public byte PollIntervalByte
    {
        get
        {
            return SNTPData[2];
        }
        set
        {
            SNTPData[2] = value;
        }
    }
    /// <summary>
    /// Maximum interval (seconds) between successive messages
    /// </summary>
    public uint PollInterval
    {
        get
        {
            // Thanks to Jim Hollenhorst <hollenho@attbi.com>
            return (uint)(Math.Pow(2, (sbyte)SNTPData[2]));
        }
    }
    /// <summary>
    /// Precision byte raw value
    /// </summary>
    public byte PrecisionByte
    {
        get
        {
            return SNTPData[3];
        }
        set
        {
            SNTPData[3] = value;
        }
    }
    /// <summary>
    /// Precision (in seconds) of the clock
    /// </summary>
    public double Precision
    {
        get
        {
            // Thanks to Jim Hollenhorst <hollenho@attbi.com>
            return (Math.Pow(2, (sbyte)SNTPData[3]));
        }
    }

    /// <summary>
    /// Round trip time (in milliseconds) to the primary reference source.
    /// </summary>
    public double RootDelay
    {
        get
        {
            int temp = 0;
            temp = 256 * (256 * (256 * SNTPData[4] + SNTPData[5]) + SNTPData[6]) + SNTPData[7];
            return 1000 * (((double)temp) / 0x10000);
        }
        set
        {
            double d = value/1000.0f*0x10000;
            int idd = (int)d;
            SNTPData[4] = (byte)(sbyte)(idd / 16777216);
            SNTPData[5] = (byte)((idd / 65536) % 256);
            SNTPData[6] = (byte)((idd / 256) % 256);
            SNTPData[7] = (byte)(idd % 256);
        }
    }

    /// <summary>
    /// Nominal error (in milliseconds) relative to the primary reference source.
    /// </summary>
    public double RootDispersion
    {
        get
        {
            int temp = 0;
            temp = 256 * (256 * (256 * SNTPData[8] + SNTPData[9]) + SNTPData[10]) + SNTPData[11];
            return 1000 * (((double)temp) / 0x10000);
        }
        set
        {
            double d = value / 1000.0f * 0x10000;
            int idd = (int)d;
            SNTPData[8] = (byte)(sbyte)(idd / 16777216);
            SNTPData[9] = (byte)((idd / 65536) % 256);
            SNTPData[10] = (byte)((idd / 256) % 256);
            SNTPData[11] = (byte)(idd % 256);
        }
    }

    /// <summary>
    /// Reference identifier (either a 4 character string or an IP address)
    /// </summary>
    public string ReferenceID
    {
        get
        {
            string val = "";
            switch (Stratum)
            {
                case Stratum.Unspecified:
                    goto case Stratum.PrimaryReference;
                case Stratum.PrimaryReference:
                    val += (char)SNTPData[offReferenceID + 0];
                    val += (char)SNTPData[offReferenceID + 1];
                    val += (char)SNTPData[offReferenceID + 2];
                    val += (char)SNTPData[offReferenceID + 3];
                    break;
                case Stratum.SecondaryReference:
                    switch (VersionNumber)
                    {
                        case 3: // Version 3, Reference ID is an IPv4 address
                            string Address = SNTPData[offReferenceID + 0].ToString() + "." +
                                             SNTPData[offReferenceID + 1].ToString() + "." +
                                             SNTPData[offReferenceID + 2].ToString() + "." +
                                             SNTPData[offReferenceID + 3].ToString();
                            try
                            {
                                IPHostEntry Host = Dns.GetHostEntry(Address);
                                val = Host.HostName + " (" + Address + ")";
                            }
                            catch (Exception)
                            {
                                val = "N/A";
                            }
                            break;
                        case 4: // Version 4, Reference ID is the timestamp of last update
                            DateTime time = ComputeDate(GetMilliSeconds(offReferenceID));
                            // Take care of the time zone                                
                            TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                            val = (time + offspan).ToString();
                            break;
                        default:
                            val = "N/A";
                            break;
                    }
                    break;
            }
            return val;
        }
    }

    /// <summary>
    /// The time at which the clock was last set or corrected
    /// </summary>
    public DateTime ReferenceTimestamp
    {
        get
        {
            DateTime time = ComputeDate(GetMilliSeconds(offReferenceTimestamp));
            // Take care of the time zone
            TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            return time + offspan;
        }
        set
        {
            SetDate(offReferenceTimestamp, value);
        }
    }

    /// <summary>
    /// The time (T1) at which the request departed the client for the server
    /// </summary>
    public DateTime OriginateTimestamp
    {
        get
        {
            return ComputeDate(GetMilliSeconds(offOriginateTimestamp));
        }
        set
        {
            SetDate(offOriginateTimestamp, value);
        }
    }

    /// <summary>
    /// The time (T2) at which the request arrived at the server
    /// </summary>
    public DateTime ReceiveTimestamp
    {
        get
        {
            DateTime time = ComputeDate(GetMilliSeconds(offReceiveTimestamp));
            // Take care of the time zone
            TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            return time + offspan;
        }
        set
        {
            SetDate(offReceiveTimestamp, value);
        }
    }

    /// <summary>
    /// The time (T3) at which the reply departed the server for client
    /// </summary>
    public DateTime TransmitTimestamp
    {
        get
        {
            DateTime time = ComputeDate(GetMilliSeconds(offTransmitTimestamp));
            // Take care of the time zone
            TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            return time + offspan;
        }
        set
        {
            SetDate(offTransmitTimestamp, value);
        }
    }

    /// <summary>
    /// Destination Timestamp (T4)
    /// </summary>
    public DateTime DestinationTimestamp;

    /// <summary>
    /// The time (in milliseconds) between the departure of request and arrival of reply 
    /// </summary>
    public double RoundTripDelay
    {
        get
        {
            // Thanks to DNH <dnharris@csrlink.net>
            TimeSpan span = (DestinationTimestamp - OriginateTimestamp) - (ReceiveTimestamp - TransmitTimestamp);
            return span.TotalMilliseconds;
        }
    }

    /// <summary>
    /// The offset (in milliseconds) of the local clock relative to the primary reference source
    /// </summary>
    public double LocalClockOffset
    {
        get
        {
            // Thanks to DNH <dnharris@csrlink.net>
            TimeSpan span = (ReceiveTimestamp - OriginateTimestamp) + (TransmitTimestamp - DestinationTimestamp);
            return (span.TotalMilliseconds / 2);
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Compute date, given the number of milliseconds since January 1, 1900
    /// </summary>
    private DateTime ComputeDate(ulong milliseconds)
    {
        TimeSpan span = TimeSpan.FromMilliseconds((double)milliseconds);
        DateTime time = new DateTime(1900, 1, 1);
        time += span;
        return time;
    }

    /// <summary>
    /// Compute the number of milliseconds, given the offset of a 8-byte array
    /// </summary>
    private ulong GetMilliSeconds(byte offset)
    {
        ulong intpart = 0, fractpart = 0;

        for (int i = 0; i <= 3; i++)
        {
            intpart = 256 * intpart + SNTPData[offset + i];
        }
        for (int i = 4; i <= 7; i++)
        {
            fractpart = 256 * fractpart + SNTPData[offset + i];
        }
        ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
        return milliseconds;
    }

    /// <summary>
    /// Set the date part of the SNTP data
    /// </summary>
    /// <param name="offset">Offset at which the date part of the SNTP data is</param>
    /// <param name="date">The date</param>
    private void SetDate(byte offset, DateTime date)
    {
        ulong intpart = 0, fractpart = 0;
        DateTime StartOfCentury = new DateTime(1900, 1, 1, 0, 0, 0);    // January 1, 1900 12:00 AM

        ulong milliseconds = (ulong)(date - StartOfCentury).TotalMilliseconds;
        intpart = milliseconds / 1000;
        fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000;

        ulong temp = intpart;
        for (int i = 3; i >= 0; i--)
        {
            SNTPData[offset + i] = (byte)(temp % 256);
            temp = temp / 256;
        }

        temp = fractpart;
        for (int i = 7; i >= 4; i--)
        {
            SNTPData[offset + i] = (byte)(temp % 256);
            temp = temp / 256;
        }
    }

    /// <summary>
    /// Returns true if received data is valid and if comes from a NTP-compliant time server.
    /// </summary>
    private bool IsResponseValid()
    {
        if (SNTPData.Length < SNTPDataLength || Mode != Mode.Server)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Initialize the SNTP client data. Sets up data structure and prepares for connection.
    /// </summary>
    private void Initialize()
    {
        // Set version number to 4 and Mode to 3 (client)
        SNTPData[0] = 0x1B;
        // Initialize all other fields with 0
        for (int i = 1; i < 48; i++)
        {
            SNTPData[i] = 0;
        }
        // Initialize the transmit timestamp
        TransmitTimestamp = GetCurrentTime();
    }

    private DateTime GetCurrentTime() => DateTime.Now;

    private void CopyTimeStamp(int offSource,int offDestination)
    {
        for (int i=0; i<4; i++)
        {
            SNTPData[offDestination + i] = SNTPData[offSource + i];
        }
    }

    #endregion

    #region Public methods
    /// <summary>
    /// Starts to the time server 
    ///	It can also update the system time.
    /// </summary>
    public async void StartService()
    {
        IPEndPoint listenEP = new IPEndPoint(IPAddress.Any, 123);
        Socket recvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        byte[] raw = new byte[SNTPDataLength];
        Memory<byte> buffer = new Memory<byte>(raw);

        try
        {            
            recvSocket.Bind(listenEP);
            //Initialize();
            
            // Timeout code
            while (true)
            {
                SocketReceiveFromResult recvResult = await recvSocket.ReceiveFromAsync(buffer, listenEP, cts.Token);
                if (cts.IsCancellationRequested) break;

                //Thread.Sleep(500);
                if (recvResult.ReceivedBytes==SNTPDataLength)
                //if (buffer.Length== SNTPDataLength)
                {
                    buffer.Span.CopyTo(SNTPData);

                    Stratum = Stratum.PrimaryReference;
                    LeapIndicator = LeapIndicator.Alarm;
                    Mode = Mode.Server;
                    PrecisionByte = 0xE7;
                    CopyTimeStamp(offTransmitTimestamp, offOriginateTimestamp);
                    DateTime dt = DateTime.UtcNow;
                    ReceiveTimestamp = dt;
                    TransmitTimestamp = dt;
                    SNTPData[offReferenceID] = (byte)'C';
                    SNTPData[offReferenceID + 1] = (byte)'O';
                    SNTPData[offReferenceID + 2] = (byte)'M';
                    SNTPData[offReferenceID + 3] = (byte)'P';

                    recvSocket.SendTo(SNTPData, SNTPData.Length, SocketFlags.None, recvResult.RemoteEndPoint);
                }               
            }
            DestinationTimestamp = GetCurrentTime();
        }
        catch (SocketException e)
        {
            throw new Exception(e.Message);
        }
        finally
        {
            recvSocket.Close();
        }
    }
    public async void StopService()
    {
        await cts.CancelAsync();
    }
    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    public override string ToString()
    {
        var str = new StringBuilder();

        str.Append("Leap indicator     : ");
        switch (LeapIndicator)
        {
            case LeapIndicator.NoWarning:
                str.AppendLine("No warning");
                break;
            case LeapIndicator.LastMinute61:
                str.AppendLine("Last minute has 61 seconds");
                break;
            case LeapIndicator.LastMinute59:
                str.AppendLine("Last minute has 59 seconds");
                break;
            case LeapIndicator.Alarm:
                str.AppendLine("Alarm Condition (clock not synchronized)");
                break;
        }
        str.AppendLine($"Version number     : {VersionNumber}");
        str.Append("Mode               : ");
        switch (Mode)
        {
            case Mode.Unknown:
                str.AppendLine("Unknown");
                break;
            case Mode.SymmetricActive:
                str.AppendLine("Symmetric Active");
                break;
            case Mode.SymmetricPassive:
                str.AppendLine("Symmetric Pasive");
                break;
            case Mode.Client:
                str.AppendLine("Client");
                break;
            case Mode.Server:
                str.AppendLine("Server");
                break;
            case Mode.Broadcast:
                str.AppendLine("Broadcast");
                break;
        }
        str.Append("Stratum            : ");
        switch (Stratum)
        {
            case Stratum.Unspecified:
            case Stratum.Reserved:
                str.AppendLine("Unspecified");
                break;
            case Stratum.PrimaryReference:
                str.AppendLine("Primary reference");
                break;
            case Stratum.SecondaryReference:
                str.AppendLine("Secondary reference");
                break;
        }

        str.AppendLine($"Precision          : {Precision} s.");
        str.AppendLine($"Poll interval      : {PollInterval} s.");
        str.AppendLine($"Reference ID       : {ReferenceID}");
        str.AppendLine($"Root delay         : {RootDelay} ms.");
        str.AppendLine($"Root dispersion    : {RootDispersion} ms.");
        str.AppendLine($"Round trip delay   : {RoundTripDelay} ms.");
        str.AppendLine($"Local clock offset : {LocalClockOffset} ms.");
        str.AppendLine($"Local time         : {GetCurrentTime().AddMilliseconds(LocalClockOffset)}");
        str.AppendLine();

        return str.ToString();
    }
    #endregion

}
