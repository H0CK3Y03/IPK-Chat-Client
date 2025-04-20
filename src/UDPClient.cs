using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

public class UdpChatClient : ITransportClient
{
    public int Retransmissions;
    public int Timeout;

    private UdpClient _udp;
    private IPEndPoint _serverEndpoint;
    private IPEndPoint? _dynamicServerEndpoint = null;

    private readonly HashSet<ushort> _receivedMessageIds = new();
    private ushort _nextMessageId = 0;

    private readonly Channel<byte[]> _messageChannel = Channel.CreateUnbounded<byte[]>();
    private readonly Channel<ushort> _confirmChannel = Channel.CreateUnbounded<ushort>();

    public UdpChatClient(string serverAddress, int serverPort, int timeout, int retransmissions)
    {
        _udp = new UdpClient(0);
        _serverEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
        Timeout = timeout;
        Retransmissions = retransmissions;

        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await ReceiveLoopAsync();
                }
                catch (Exception ex)
                {
                    Debugger.PrintError($"Fatal in ReceiveLoop: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        });
    }

    public Task ConnectAsync() => Task.CompletedTask;
    public Task DisconnectAsync() => Task.CompletedTask;

    public async Task SendAsync(string msg)
    {
        try
        {
            byte type = GetMessageTypeByte(msg);
            ushort messageId = _nextMessageId++;
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg + '\0');
            byte[] header = BuildHeader(type, messageId);
            byte[] fullPacket = header.Concat(msgBytes).ToArray();

            IPEndPoint sendEndpoint = _dynamicServerEndpoint ?? _serverEndpoint;

            for (int attempt = 0; attempt <= Retransmissions; attempt++)
            {
                await _udp.SendAsync(fullPacket, fullPacket.Length, sendEndpoint);

                using var cts = new CancellationTokenSource(Timeout);
                try
                {
                    while (true)
                    {
                        var confirmId = await _confirmChannel.Reader.ReadAsync(cts.Token);
                        if (confirmId == messageId)
                            return;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout occurred, retry
                }
            }

            throw new TimeoutException($"No CONFIRM received after retries for message ID {messageId}");
        }
        catch (SocketException ex) when (
            ex.SocketErrorCode == SocketError.ConnectionReset ||
            ex.SocketErrorCode == SocketError.ConnectionAborted ||
            ex.SocketErrorCode == SocketError.Shutdown ||
            ex.SocketErrorCode == SocketError.NotConnected)
        {
            Debugger.PrintError($"Socket exception: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debugger.PrintError($"Failed to send UDP message: {ex.Message}");
        }
    }

    public async Task<string?> ReceiveAsync()
    {
        while (true)
        {
            var data = await _messageChannel.Reader.ReadAsync();

            if (data.Length < 3)
                continue;

            byte type = data[0];
            ushort messageId = (ushort)((data[1] << 8) | data[2]);

            if (_receivedMessageIds.Contains(messageId))
            {
                await SendConfirmAsync(messageId, _dynamicServerEndpoint ?? _serverEndpoint);
                continue;
            }

            _receivedMessageIds.Add(messageId);
            await SendConfirmAsync(messageId, _dynamicServerEndpoint ?? _serverEndpoint);

            if (type == 0x01) // REPLY
            {
                if (data.Length < 4) continue;

                byte resultCode = data[3];
                string replyContent = Encoding.UTF8.GetString(data[4..]).TrimEnd('\0');

                return $"REPLY {(resultCode == 1 ? "SUCCESS" : "FAILURE")}: {replyContent}";
            }

            string messageBody = Encoding.UTF8.GetString(data[3..]).TrimEnd('\0');
            return messageBody;
        }
    }

    private async Task ReceiveLoopAsync()
    {
        while (true)
        {
            var result = await _udp.ReceiveAsync();
            byte[] data = result.Buffer;

            if (_dynamicServerEndpoint == null && data.Length > 0 && data[0] == 0x01)
            {
                _dynamicServerEndpoint = result.RemoteEndPoint;
            }

            if (data.Length == 3 && data[0] == 0x00)
            {
                ushort confirmId = (ushort)((data[1] << 8) | data[2]);
                await _confirmChannel.Writer.WriteAsync(confirmId);
            }
            else
            {
                await _messageChannel.Writer.WriteAsync(data);
            }
        }
    }

    private async Task SendConfirmAsync(ushort messageId, IPEndPoint target)
    {
        byte[] confirmMsg = new byte[] { 0x00, (byte)(messageId >> 8), (byte)(messageId & 0xFF) };
        await _udp.SendAsync(confirmMsg, confirmMsg.Length, target);
    }

    private byte[] BuildHeader(byte type, ushort messageId)
    {
        return new byte[] { type, (byte)(messageId >> 8), (byte)(messageId & 0xFF) };
    }

    private byte GetMessageTypeByte(string msg)
    {
        string prefix = msg.Split(' ', 2)[0].ToUpper();
        return prefix switch
        {
            "CONFIRM" => 0x00,
            "REPLY" => 0x01,
            "AUTH" => 0x02,
            "JOIN" => 0x03,
            "MSG" => 0x04,
            "PING" => 0xFD,
            "ERR" => 0xFE,
            "BYE" => 0xFF,
            _ => throw new InvalidOperationException($"Unknown message prefix: {prefix}")
        };
    }

    public void Close()
    {
        _udp.Close();
    }
}
