public class Debugger
{
    public static bool DebugMode { get; set; } = false;

    public static void PrintError(string message)
    {
            Console.WriteLine($"ERROR: {message}\n");
    }

    public static void PrintReceivedError(string? message, string? displayName)
    {
        Console.WriteLine($"ERROR FROM {displayName}: {message}");
    }

    public static void PrintReplyOK(string? message)
    {
        Console.WriteLine($"Action Success: {message}");
    }

    public static void PrintReplyNOK(string? message)
    {
        Console.WriteLine($"Action Failure: {message}");
    }

    public static void PrintReceivedMessage(string? message, string? displayName)
    {
        Console.WriteLine($"{displayName}: {message}");
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
            Console.Error.WriteLine($"<{message}> Length: {message.Length}");
            Console.Error.WriteLine(string.Join("|", message.Select(c => ((int)c).ToString())));
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