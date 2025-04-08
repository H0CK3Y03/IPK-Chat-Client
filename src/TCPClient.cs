using System.Net.Sockets;
using System.Text;

public class TcpChatClient : ITransportClient
{
    private readonly string _hostname;
    private readonly int _port;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public TcpChatClient(string hostname, int port)
    {
        _hostname = hostname;
        _port = port;
        _tcpClient = new TcpClient();
    }

    // Connect asynchronously to the server and prepare communication streams
    public async Task ConnectAsync()
    {
        try
        {
            if (_tcpClient == null)
            {
                throw new InvalidOperationException("TcpClient is not initialized.");
            }
            
            await _tcpClient.ConnectAsync(_hostname, _port);  // Connect using the existing TcpClient instance
            InitializeStreams();
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"Connection failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // Initialize network streams and readers/writers
    private void InitializeStreams()
    {
        _stream = _tcpClient?.GetStream();
        _reader = new StreamReader(_stream!, Encoding.UTF8);
        _writer = new StreamWriter(_stream!, Encoding.UTF8) { NewLine = "\r\n", AutoFlush = true };
    }

    // Send a message asynchronously to the server
    public async Task SendAsync(string message)
    {
        if (_writer == null)
        {
            Debugger.PrintError("Attempted to send before connection was initialized.");
            return;
        }

        try
        {
            await _writer.WriteLineAsync(message);
            if (Debugger.DebugMode)
                Debugger.PrintStatus($"Sent: {message}");
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"Failed to send message: {ex.Message}");
        }
    }

    // Receive a message asynchronously from the server
    public async Task<string?> ReceiveAsync()
    {
        if (_reader == null)
        {
            Debugger.PrintError("Attempted to receive before connection was initialized.");
            return null;
        }

        try
        {
            string? response = await _reader.ReadLineAsync();
            return response;
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"Failed to receive message: {ex.Message}");
            return null;
        }
    }

    // Disconnect gracefully by sending a "BYE" message and closing the connection
    public async Task DisconnectAsync()
    {
        _tcpClient?.Close(); // Properly close the TcpClient connection
        Debugger.PrintStatus("Disconnected from server.");
        await Task.CompletedTask; // Ensure the method is properly asynchronous
    }
}
