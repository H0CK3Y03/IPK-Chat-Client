public class Debugger
{
    public static bool DebugMode { get; set; } = false;

    public static void PrintError(string message)
    {
            Console.WriteLine($"ERROR: {message}\n");
    }

    public static void PrintWarning(string message)
    {
        Console.Error.WriteLine($"WARNING: {message}");
    }

    public static void PrintStatus(string message)
    {
        if (DebugMode)
        {
            Console.Error.WriteLine($"STATUS: {message}");
        }
    }

    public static void PrintInfo()
    {
        if (DebugMode)
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

    public static void PrintHelp()
    {
        Console.WriteLine($"""
        HELP:
        /auth <username> <secret> <displayname>
        /join <channelid>
        /rename <displayname>
        /msg <message>
        /bye
        /help
        /error <message>
        """);
    }
}