public class Debugger
{
    public static bool DebugMode { get; set; } = false;

    public static void PrintError(string message)
    {
        Console.Error.WriteLine($"ERROR: {message}");
    }

    public static void PrintWarning(string message)
    {
        Console.WriteLine($"WARNING: {message}");
    }

    public static void PrintInfo()
    {
        Console.WriteLine($"""
        INFO:
          Transport Protocol   : {ArgParser.TransportProtocol}
          Server/IP Address    : {ArgParser.ServerAddress}
          Port                 : {ArgParser.Port}
          UDP Timeout          : {ArgParser.Timeout} ms
          UDP Retransmissions  : {ArgParser.Retransmissions}
        """);
    }
}