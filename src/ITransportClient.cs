public interface ITransportClient
{
    Task ConnectAsync();  // Establishes the connection asynchronously
    Task SendAsync(string message);  // Sends a message asynchronously
    Task<string?> ReceiveAsync();  // Receives a message asynchronously
    Task DisconnectAsync();  // Disconnects from the server asynchronously
}
