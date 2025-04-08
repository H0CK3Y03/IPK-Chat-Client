using System.Diagnostics;

public class ChatClientFSM
{
    private readonly ITransportClient _client;
    private ClientState _state = ClientState.start;

    public string _displayName = string.Empty;
    private Task<string?>? _receiveTask = null;
    private Task<string?>? _userInputTask = null;

    public enum ClientState
    {
        start,
        auth,
        open,
        join,
        end
    }

    public ChatClientFSM(ITransportClient client)
    {
        _client = client;
    }

    public async Task RunAsync()
    {
        try
        {
            while (!CancellationSource.Token.IsCancellationRequested)
            {
                switch (_state)
                {
                    case ClientState.start:
                        await StartStateAsync();
                        break;
                    case ClientState.auth:
                        await AuthStateAsync();
                        break;
                    case ClientState.open:
                        await OpenStateAsync();
                        break;
                    case ClientState.join:
                        await JoinStateAsync();
                        break;
                    case ClientState.end:
                        await EndStateAsync();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_state), _state, null); 
                }
            }
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"{ex.Message}");
            Environment.Exit(1);
        }
    }

    // Start state logic
    private async Task StartStateAsync()
    {
        try 
        {
            Debugger.PrintStatus("Entered start state.");
            // string? serverReply = await CancellationHelper.WithCancellation(_client.ReceiveAsync(), CancellationSource.Token);
            // Start tasks for receiving data and reading user input.
            if (_receiveTask == null || _receiveTask.IsCompleted)
            {
                _receiveTask = _client.ReceiveAsync();
            }
            if (_userInputTask == null || _userInputTask.IsCompleted)
            {
                _userInputTask = ClientMessageBuilder.ReadUserInputAsync();
            }
            // Wait for either task to complete (first to complete).
            var completedTask = await Task.WhenAny(_receiveTask, _userInputTask);
            // Check if the completed task is the receive task or user input task
            if (completedTask == _receiveTask)
            {
                // Process server reply if it's received
                string? serverReply = await _receiveTask;
                if (string.IsNullOrEmpty(serverReply))
                {
                    Debugger.PrintWarning("Server did not reply.");
                    return;
                }
                else
                {
                    Debugger.PrintStatus($"Server Reply: {serverReply}");

                    // Check if server sends an error or BYE message.
                    if (serverReply.StartsWith("ERR") || serverReply.StartsWith("BYE"))
                    {
                        Debugger.PrintStatus("Exiting due to server error or BYE message.");
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                }
                // Restart the receive task for the next message
                _receiveTask = _client.ReceiveAsync();
            }
            // Check if the completed task is the user input task
            else
            if (completedTask == _userInputTask)
            {
                // Process user input if it's received
                string? userInput = await _userInputTask;
                while (string.IsNullOrEmpty(userInput) && !_receiveTask.IsCompleted)
                {
                    // Goes back into FSM and doesn't change state
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }

                // Parse user input command
                var command = new CommandParser();
                if (string.IsNullOrEmpty(userInput))
                {
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }
                var parsed = command.Parse(userInput);

                if (parsed == null)
                {
                    Debugger.PrintWarning("No Input.");
                    return;
                }

                if (parsed.Type == CommandParser.CommandType.Bye)
                {
                    Debugger.PrintStatus("Bye command entered. Exiting...");
                    // Send BYE message to server
                    string byeMsg = ClientMessageBuilder.BuildBye(_displayName);
                    await _client.SendAsync(byeMsg);
                    // Cancel the operation and exit
                    CancellationSource.Cancel();
                    _state = ClientState.end;
                    await EndStateAsync();
                    return;
                }

                if (parsed.Type == CommandParser.CommandType.Invalid)
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
                if (parsed.Type == CommandParser.CommandType.Error)
                {
                    Debugger.PrintWarning("Error command entered. Exiting...");
                    // Send error message to server of manually sent error
                    string byeMsg = ClientMessageBuilder.BuildError(_displayName, parsed.Payload);
                    await _client.SendAsync(byeMsg);
                    // Cancel the operation and exit
                    CancellationSource.Cancel();
                    _state = ClientState.end;
                    await EndStateAsync();
                    return;
                }
                if (parsed.Type == CommandParser.CommandType.Help)
                {
                    Debugger.PrintHelp();
                    return;
                }
                if (parsed.Type == CommandParser.CommandType.Msg)
                {
                    Debugger.PrintWarning("You need to be authenticated to send messages.");
                    return;
                }
                if (parsed.Type == CommandParser.CommandType.Rename)
                {
                    _displayName = parsed.DisplayName ?? string.Empty;
                    return;
                }

                if (string.IsNullOrEmpty(parsed.Username) || string.IsNullOrEmpty(parsed.Secret) || string.IsNullOrEmpty(parsed.DisplayName))
                {
                    Debugger.PrintWarning("Invalid auth command. Please try again.");
                    return;
                }

                // Send authentication message to server
                string authMsg = ClientMessageBuilder.BuildAuth(parsed.Username, _displayName = parsed.DisplayName, parsed.Secret);
                // Check if a server message was already received before proceeding with user input
                if (_receiveTask.IsCompleted)
                {
                    string? serverReply = await _receiveTask;
                    if (!string.IsNullOrEmpty(serverReply) && (serverReply.StartsWith("ERR") || serverReply.StartsWith("BYE")))
                    {
                        Debugger.PrintStatus("Exiting due to server error or BYE message.");
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    _receiveTask = _client.ReceiveAsync();
                }
                await _client.SendAsync(authMsg);

                // If the message was fine, proceed to the auth state
                _state = ClientState.auth;
            }
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"in StartStateAsync: {ex.Message}");
            // Build and send error message to server if an exception occurs
            string errMsg = ClientMessageBuilder.BuildError(_displayName, ex.Message);
            await _client.SendAsync(errMsg);
            // Close the connection and exit
            CancellationSource.Cancel();
            _state = ClientState.end;
            await EndStateAsync();
            Environment.Exit(1);
        }
    }

    // Auth state logic
    private async Task AuthStateAsync()
    {
        Debugger.PrintStatus("Entered auth state.");
        Console.WriteLine("START IS DONE!");
        string? authResponse = await _client.ReceiveAsync();
        if (!string.IsNullOrEmpty(authResponse))
        {
            Debugger.PrintStatus($"Authentication response: {authResponse}");
            if (authResponse == "ERR" || authResponse == "BYE")
            {
                CancellationSource.Cancel();
                _state = ClientState.end;
            }
            else
            {
                _state = ClientState.open;
            }
        }
    }

    // Open state logic
    private async Task OpenStateAsync()
    {
        Debugger.PrintStatus("Entered open state.");
        Console.WriteLine("Enter channel ID to join:");
        var channelId = Console.ReadLine();
        string joinMsg = $"JOIN {channelId}";
        await _client.SendAsync(joinMsg);
        _state = ClientState.join;
    }

    // Join state logic
    private async Task JoinStateAsync()
    {
        Debugger.PrintStatus("Entered join state.");
        string? joinResponse = await _client.ReceiveAsync();
        if (!string.IsNullOrEmpty(joinResponse))
        {
            Console.WriteLine($"[SERVER] {joinResponse}");
            if (joinResponse == "ERR" || joinResponse == "BYE")
            {
                CancellationSource.Cancel();
                _state = ClientState.end;
            }
            else
            {
                // Proceed to sending messages
                _state = ClientState.open;
            }
        }
    }

    // End state logic (disconnection)
    private async Task EndStateAsync()
    {
        Debugger.PrintStatus("Entered end state.");
        await _client.DisconnectAsync();
        Debugger.PrintStatus("Disconnected. Exiting...");
    }
}
