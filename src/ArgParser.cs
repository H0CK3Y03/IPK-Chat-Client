public class ArgParser
{
    // Static fields to store configuration values
    // Default values are provided here
    public static string TransportProtocol { get; set; } = "";  // Default is empty string (must be provided by user)
    public static string ServerAddress { get; set; } = "";  // Default is empty string (must be provided by user)
    public static int Port { get; set; } = 4567;  // Default port is 4567
    public static int Timeout { get; set; } = 250;  // Default UDP confirmation timeout is 250ms
    public static int Retransmissions { get; set; } = 3;  // Default max retransmissions is 3

    public static void ParseArguments(string[] args)
    {
        int argc = args.Length;
        if (argc == 0)
        {
            Debugger.PrintError("No arguments provided.");
            Environment.Exit(1);
        }
        if (args.Contains("-h") || args.Contains("--help"))
        {
            if (argc != 1)
            {
                Debugger.PrintError("Invalid argument combination. Use -h or --help alone.");
                Environment.Exit(1);
            }
            PrintHelp();
            Environment.Exit(0);
        }

        // Parse mandatory arguments
        if (args.Contains("-t")) // Transport protocol
        {
            if (args[Array.IndexOf(args, "-t") + 1] != "tcp" && args[Array.IndexOf(args, "-t") + 1] != "udp")
            {
                Debugger.PrintError("Invalid transport protocol. Use 'tcp' or 'udp'.");
                Environment.Exit(1);
                return;
            }
            TransportProtocol = args[Array.IndexOf(args, "-t") + 1]; // Get value after -t
        }
        else
        {
            Debugger.PrintError("Missing mandatory argument -t (Transport protocol).");
            Environment.Exit(1);
        }

        if (args.Contains("-s")) // Server address
        {
            ServerAddress = args[Array.IndexOf(args, "-s") + 1]; // Get value after -s
        }
        else
        {
            Debugger.PrintError("Missing mandatory argument -s (Server address).");
            Environment.Exit(1);
        }

        // Parse optional arguments with defaults
        if (args.Contains("-p")) // Port
        {
            Port = int.Parse(args[Array.IndexOf(args, "-p") + 1]);
        }

        if (args.Contains("-d")) // Timeout
        {
            Timeout = int.Parse(args[Array.IndexOf(args, "-d") + 1]);
        }

        if (args.Contains("-r")) // Retransmissions
        {
            Retransmissions = int.Parse(args[Array.IndexOf(args, "-r") + 1]);
        }
        if (args.Contains("-g") || args.Contains("--debug")) // Debug mode
        {
            Debugger.DebugMode = true;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: ./ipk25-client -t <tcp|udp> -s <address> [-p <port>] [-d <timeout>] [-r <retries>] [-h] [-g]");
        Console.WriteLine("-t <tcp|udp>   : Transport protocol (required)");
        Console.WriteLine("-s <address>   : Server IP or hostname (required)");
        Console.WriteLine("-p <port>      : Server port (default: 4567)");
        Console.WriteLine("-d <timeout>   : UDP confirmation timeout in ms (default: 250)");
        Console.WriteLine("-r <retries>   : Max UDP retransmissions (default: 3)");
        Console.WriteLine("-h or --help   : Print help");
        Console.WriteLine("-g or --debug  : Enable debug mode");
    }
}