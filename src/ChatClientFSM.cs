using System.Diagnostics;

public class ChatClientFSM
{
    private readonly ITransportClient _client;
    private ClientState _state = ClientState.start;

    public string _displayName = "unknown"; // unknown by default
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
                    // Debugger.PrintError("Server did not reply.");
                    return;
                }
                else
                {
                    Debugger.PrintStatus($"Server Reply: {serverReply}");

                    var receivedMessage = new ClientMessageHandler();
                    var parsedMessage = receivedMessage.HandleMessage(serverReply);
                    if (parsedMessage == null || parsedMessage.Type == ClientMessageHandler.CommandType.Malformed)
                    {
                        throw new Exception("Received malformed message.");
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Bye)
                    {
                        Debugger.PrintStatus("BYE message received. Exiting...");
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Err)
                    {
                        Debugger.PrintStatus("ERR message received. Exiting...");
                        Debugger.PrintReceivedError(parsedMessage.Content, parsedMessage.DisplayName);
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        // Do nothing
                    }
                }
                // Restart the receive task for the next message
                _receiveTask = _client.ReceiveAsync();
            }
            // Check if the completed task is the user input task
            else if (completedTask == _userInputTask)
            {
                // Process user input if it's received
                string? userInput = await _userInputTask;
                while (string.IsNullOrEmpty(userInput) && !_receiveTask.IsCompleted)
                {
                    // Goes back into FSM and doesn't change state
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }
                if (string.IsNullOrEmpty(userInput))
                {
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }
                // Parse user input command
                var command = new CommandParser();
                var parsed = command.Parse(userInput);
                if (parsed == null)
                {
                    Debugger.PrintWarning("No Input.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Bye)
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
                else if (parsed.Type == CommandParser.CommandType.Invalid)
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Error)
                {
                    Debugger.PrintStatus("Error command entered. Exiting...");
                    throw new Exception($"{parsed.Content}");
                }
                else if (parsed.Type == CommandParser.CommandType.Help)
                {
                    Debugger.PrintHelp();
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Msg)
                {
                    Debugger.PrintWarning("You need to be authenticated to send messages.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Rename)
                {
                    _displayName = parsed.DisplayName ?? string.Empty;
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Auth)
                {
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
                        if (!string.IsNullOrEmpty(serverReply) && (serverReply.ToLower().StartsWith("err") || serverReply.ToLower().StartsWith("bye")))
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
                    _state = ClientState.auth;
                }
                else if (parsed.Type == CommandParser.CommandType.Join)
                {
                    Debugger.PrintWarning("You need to be authenticated to join a channel.");
                    return;
                }
                else
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"{ex.Message}");
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
        try 
        {
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
                    // Debugger.PrintError("Server did not reply.");
                    return;
                }
                else
                {
                    Debugger.PrintStatus($"Server Reply: {serverReply}");

                    var receivedMessage = new ClientMessageHandler();
                    var parsedMessage = receivedMessage.HandleMessage(serverReply);
                    if (parsedMessage == null || parsedMessage.Type == ClientMessageHandler.CommandType.Malformed)
                    {
                        throw new Exception("Received malformed message.");
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Bye)
                    {
                        Debugger.PrintStatus("BYE message received. Exiting...");
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Err)
                    {
                        Debugger.PrintStatus("ERR message received. Exiting...");
                        Debugger.PrintReceivedError(parsedMessage.Content, parsedMessage.DisplayName);
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Msg)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        throw new Exception($"{parsedMessage.Content}");
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.ReplyOK)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        Debugger.PrintReplyOK(parsedMessage.Content);
                        _state = ClientState.open;
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.ReplyNOK)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        Debugger.PrintReplyNOK(parsedMessage.Content);
                        // Do nothing
                        return;
                    }
                    else
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        // Do nothing
                    }
                }
                // Restart the receive task for the next message
                _receiveTask = _client.ReceiveAsync();
            }
            // Check if the completed task is the user input task
            else if (completedTask == _userInputTask)
            {
                // Process user input if it's received
                string? userInput = await _userInputTask;
                while (string.IsNullOrEmpty(userInput) && !_receiveTask.IsCompleted)
                {
                    // Goes back into FSM and doesn't change state
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }

                if (string.IsNullOrEmpty(userInput))
                {
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }
                // Parse user input command
                var command = new CommandParser();
                var parsed = command.Parse(userInput);

                if (parsed == null)
                {
                    Debugger.PrintWarning("No Input.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Bye)
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
                else if (parsed.Type == CommandParser.CommandType.Invalid)
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Error)
                {
                    Debugger.PrintStatus("Error command entered. Exiting...");
                    throw new Exception($"{parsed.Content}");
                }
                else if (parsed.Type == CommandParser.CommandType.Help)
                {
                    Debugger.PrintHelp();
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Msg)
                {
                    Debugger.PrintWarning("You need to be authenticated to send messages.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Rename)
                {
                    _displayName = parsed.DisplayName ?? string.Empty;
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Join)
                {
                    Debugger.PrintWarning("You need to be authenticated to join a channel.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Auth)
                {
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
                        if (!string.IsNullOrEmpty(serverReply) && (serverReply.ToLower().StartsWith("err") || serverReply.ToLower().StartsWith("bye")))
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
                }
                else
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"{ex.Message}");
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

    // Open state logic
    private async Task OpenStateAsync()
    {
        Debugger.PrintStatus("Entered open state.");
        try 
        {
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
                    // Debugger.PrintError("Server did not reply.");
                    return;
                }
                else
                {
                    Debugger.PrintStatus($"Server Reply: {serverReply}");

                    var receivedMessage = new ClientMessageHandler();
                    var parsedMessage = receivedMessage.HandleMessage(serverReply);
                    if (parsedMessage == null || parsedMessage.Type == ClientMessageHandler.CommandType.Malformed)
                    {
                        throw new Exception("Received malformed message.");
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Bye)
                    {
                        Debugger.PrintStatus("BYE message received. Exiting...");
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Err)
                    {
                        Debugger.PrintStatus("ERR message received. Exiting...");
                        Debugger.PrintReceivedError(parsedMessage.Content, parsedMessage.DisplayName);
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Msg)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        Debugger.PrintReceivedMessage(parsedMessage.Content, parsedMessage.DisplayName);
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.ReplyOK)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        throw new Exception($"{parsedMessage.Content}");
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.ReplyNOK)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        throw new Exception($"{parsedMessage.Content}");
                    }
                    else
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        // Do nothing
                    }
                }
                // Restart the receive task for the next message
                _receiveTask = _client.ReceiveAsync();
            }
            // Check if the completed task is the user input task
            else if (completedTask == _userInputTask)
            {
                // Process user input if it's received
                string? userInput = await _userInputTask;
                while (string.IsNullOrEmpty(userInput) && !_receiveTask.IsCompleted)
                {
                    // Goes back into FSM and doesn't change state
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }

                if (string.IsNullOrEmpty(userInput))
                {
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }
                // Parse user input command
                var command = new CommandParser();
                var parsed = command.Parse(userInput);

                if (parsed == null)
                {
                    Debugger.PrintWarning("No Input.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Bye)
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
                else if (parsed.Type == CommandParser.CommandType.Invalid)
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Error)
                {
                    Debugger.PrintStatus("Error command entered. Exiting...");
                    throw new Exception($"{parsed.Content}");
                }
                else if (parsed.Type == CommandParser.CommandType.Help)
                {
                    Debugger.PrintHelp();
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Msg)
                {
                    if (string.IsNullOrEmpty(parsed.Content))
                    {
                        Debugger.PrintWarning("No message provided.");
                        return;
                    }
                    // Send message to server
                    string msg = ClientMessageBuilder.BuildMsg(_displayName, parsed.Content);
                    await _client.SendAsync(msg);
                    Debugger.PrintStatus($"Message sent: {parsed.Content}");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Rename)
                {
                    _displayName = parsed.DisplayName ?? string.Empty;
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Join)
                {
                    if (string.IsNullOrEmpty(parsed.ChannelId))
                    {
                        Debugger.PrintWarning("No channel ID provided.");
                        return;
                    }
                    string joinMsg = ClientMessageBuilder.BuildJoin(parsed.ChannelId, _displayName);
                    await _client.SendAsync(joinMsg);
                    Debugger.PrintStatus($"Join command sent: {parsed.ChannelId}");
                    _state = ClientState.join;
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Auth)
                {
                    Debugger.PrintWarning("You are already authenticated.");
                    return;
                }
                else
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"{ex.Message}");
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

    // Join state logic
    private async Task JoinStateAsync()
    {
        Debugger.PrintStatus("Entered join state.");
        try 
        {
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
                    // Debugger.PrintError("Server did not reply.");
                    return;
                }
                else
                {
                    Debugger.PrintStatus($"Server Reply: {serverReply}");

                    var receivedMessage = new ClientMessageHandler();
                    var parsedMessage = receivedMessage.HandleMessage(serverReply);
                    if (parsedMessage == null || parsedMessage.Type == ClientMessageHandler.CommandType.Malformed)
                    {
                        throw new Exception("Received malformed message.");
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Bye)
                    {
                        Debugger.PrintStatus("BYE message received. Exiting...");
                        CancellationSource.Cancel();
                        _state = ClientState.end;
                        await EndStateAsync();
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Err)
                    {
                        // Do nothing
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.Msg)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        Debugger.PrintReceivedMessage(parsedMessage.Content, parsedMessage.DisplayName);
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.ReplyOK)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        Debugger.PrintReplyOK(parsedMessage.Content);
                        _state = ClientState.open;
                        return;
                    }
                    else if (parsedMessage.Type == ClientMessageHandler.CommandType.ReplyNOK)
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        Debugger.PrintReplyNOK(parsedMessage.Content);
                        _state = ClientState.open;
                        return;
                    }
                    else
                    {
                        Debugger.PrintStatus($"Received message: {parsedMessage.Content}");
                        // Do nothing
                    }
                }
                // Restart the receive task for the next message
                _receiveTask = _client.ReceiveAsync();
            }
            // Check if the completed task is the user input task
            else if (completedTask == _userInputTask)
            {
                // Process user input if it's received
                string? userInput = await _userInputTask;
                while (string.IsNullOrEmpty(userInput) && !_receiveTask.IsCompleted)
                {
                    // Goes back into FSM and doesn't change state
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }

                if (string.IsNullOrEmpty(userInput))
                {
                    Debugger.PrintWarning("No input provided for auth.");
                    return;
                }
                // Parse user input command
                var command = new CommandParser();
                var parsed = command.Parse(userInput);

                if (parsed == null)
                {
                    Debugger.PrintWarning("No Input.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Bye)
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
                else if (parsed.Type == CommandParser.CommandType.Invalid)
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Error)
                {
                    Debugger.PrintStatus("Error command entered. Exiting...");
                    throw new Exception($"{parsed.Content}");
                }
                else if (parsed.Type == CommandParser.CommandType.Help)
                {
                    Debugger.PrintHelp();
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Msg)
                {
                    Debugger.PrintWarning("You can't send messages while joining a channel.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Rename)
                {
                    _displayName = parsed.DisplayName ?? string.Empty;
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Join)
                {
                    // Do nothing
                    Debugger.PrintWarning("You are already joining a channel.");
                    return;
                }
                else if (parsed.Type == CommandParser.CommandType.Auth)
                {
                    Debugger.PrintWarning("You are already authenticated.");
                    return;
                }
                else
                {
                    Debugger.PrintWarning("Invalid command. Please try again.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"{ex.Message}");
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

    // End state logic (disconnection)
    private async Task EndStateAsync()
    {
        Debugger.PrintStatus("Entered end state.");
        await _client.DisconnectAsync();
        Debugger.PrintStatus("Disconnected. Exiting...");
    }
}
