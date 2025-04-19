public class ClientMessageBuilder
{
    public static async Task<string?> ReadUserInputAsync()
    {
        return await Task.Run(() =>
        {
            return Console.ReadLine() ?? string.Empty;
        });
    }
    public static string BuildAuth(string username, string displayName, string secret) =>
        $"AUTH {username} AS {displayName} USING {secret}\r\n";

    public static string BuildJoin(string channelId, string displayName) =>
        $"JOIN {channelId} AS {displayName}\r\n";

    public static string BuildError(string displayName, string? content) =>
        $"ERROR FROM {displayName} IS {content}\r\n";

    public static string BuildMsg(string displayName, string content) =>
        $"MSG FROM {displayName} IS {Truncate(content, 60000)}\r\n";

    public static string BuildBye(string displayName) =>
        $"BYE FROM {displayName}\r\n";

    public static string Truncate(string input, int maxLength) =>
        input.Length > maxLength ? input.Substring(0, maxLength) : input;
}
