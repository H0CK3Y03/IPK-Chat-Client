public class ClientMessageBuilder
{
    public static async Task<string?> ReadUserInputAsync()
    {
        return await Task.Run(() =>
        {
            return Console.ReadLine();
        });
    }
    public static string BuildAuth(string username, string displayName, string secret) =>
        $"AUTH {Truncate(username, 20)} AS {Truncate(displayName, 20)} USING {secret}";

    public static string BuildJoin(string channelId, string displayName) =>
        $"JOIN {channelId} AS {displayName}";

    public static string BuildError(string displayName, string? content) =>
        $"ERR FROM {displayName} IS {content}";

    public static string BuildMsg(string displayName, string content) =>
        $"MSG FROM {displayName} IS {Truncate(content, 60000)}";

    public static string BuildBye(string displayName) =>
        $"BYE FROM {displayName}";

    public static string Truncate(string input, int maxLength) =>
        input.Length > maxLength ? input.Substring(0, maxLength) : input;
}
