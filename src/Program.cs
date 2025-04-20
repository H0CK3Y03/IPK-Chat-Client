class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            ArgParser.ParseArguments(args);
            Debugger.PrintInfo();
            // create a transport client based on the selected protocol
            ITransportClient transportClient = ArgParser.TransportProtocol switch
            {
                "tcp" => new TcpChatClient(ArgParser.ServerAddress, ArgParser.Port),
                "udp" => new UdpChatClient(ArgParser.ServerAddress, ArgParser.Port, ArgParser.Timeout, ArgParser.Retransmissions),
                _ => throw new NotSupportedException($"Transport protocol {ArgParser.TransportProtocol} is not supported.")
            };
            // 1. connect to the server
            await transportClient.ConnectAsync();
            Debugger.PrintStatus($"Connected to {ArgParser.ServerAddress}:{ArgParser.Port} using {ArgParser.TransportProtocol.ToUpper()}.");

            // string? serverResponse = await transportClient.ReceiveAsync();  // Receive the response from the server
            // If connected successfully, start the FSM
            // CancellationTokenSource cts = new CancellationTokenSource();
            // CancellationToken token = cts.Token;
            var fsm = new ChatClientFSM(transportClient);
            // for Ctrl+c termination
            Console.CancelKeyPress += (sender, e) =>
            {
                Debugger.PrintStatus("\nCtrl+C pressed. Exiting...");
                // CancellationSource.Token.IsCancellationRequested = true;
                CancellationSource.Cancel();
                e.Cancel = true; // Cancel the default behavior (immediate exit)
                transportClient.SendAsync(ClientMessageBuilder.BuildBye(fsm._displayName));
                transportClient.DisconnectAsync();
                Environment.Exit(0);
            };
            await fsm.RunAsync();
        }
        catch (Exception ex)
        {
            Debugger.PrintError(ex.Message);
            Environment.Exit(1);   
        }
        
    }
}
