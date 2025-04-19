using System.Text.RegularExpressions;

public class CommandParser
{
    public enum CommandType { Auth, Msg, Join, Rename, Bye, Help, Error, Invalid }

    public class ParsedCommand
    {
        public CommandType Type;
        public string? Content;
        public string? Username;
        public string? DisplayName;
        public string? Secret;
        public string? ChannelId;
    }

    public ParsedCommand Parse(string input)
    {
        if (!input.StartsWith("/"))
        {
            if (MatchRegex(input, @"^[\x0A\x20-\x7E]{1,60000}$") == null)
            {
                return new ParsedCommand { Type = CommandType.Invalid };
            }
            return new ParsedCommand { Type = CommandType.Msg, Content = input };
        }
        if (input.ToLower().StartsWith("/auth"))
        {
            return authCommandArgParser(input);
        }
        else if (input.ToLower().StartsWith("/join"))
        {
            return joinCommandArgParser(input);
        }
        else if (input.ToLower().StartsWith("/rename"))
        {
            return renameCommandArgParser(input);
        }
        else if (input.ToLower().StartsWith("/bye"))
        {
            return new ParsedCommand { Type = CommandType.Bye };
        }
        else if (input.ToLower().StartsWith("/help"))
        {
            return new ParsedCommand { Type = CommandType.Help };
        }
        else if (input.ToLower().StartsWith("/error"))
        {
            return new ParsedCommand { Type = CommandType.Error };
        }
        else
        {
            return new ParsedCommand { Type = CommandType.Invalid };
        }
    }

    public ParsedCommand authCommandArgParser(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) 
            return new ParsedCommand { Type = CommandType.Invalid };
        //  Splint string into /auth username secret displayName
        var parts = arg.Split(' ', 4);
        if (parts == null || parts.Length != 4) 
            return new ParsedCommand { Type = CommandType.Invalid };
        // Check if username, secret and displayName are valid
        var username = MatchRegex(parts[1], @"^[A-Za-z0-9_-]{1,20}$");
        var secret = MatchRegex(parts[2], @"^[A-Za-z0-9_-]{1,128}$");
        var displayName = MatchRegex(parts[3], @"^[\x21-\x7E]{1,20}$");
        if (username == null || secret == null || displayName == null) 
            return new ParsedCommand { Type = CommandType.Invalid };

        return new ParsedCommand { Type = CommandType.Auth, Username = username, Secret = secret, DisplayName = displayName };
    }

    public ParsedCommand joinCommandArgParser(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) 
            return new ParsedCommand { Type = CommandType.Invalid };
        // Splint string into /join channelId
        var parts = arg.Split(' ', 2);
        if (parts == null || parts.Length != 2) 
            return new ParsedCommand { Type = CommandType.Invalid };
        // Check if channelId is valid
        var channelId = MatchRegex(parts[1], @"^[A-Za-z0-9_-]{1,20}$");
        if (channelId == null) 
            return new ParsedCommand { Type = CommandType.Invalid };

        return new ParsedCommand { Type = CommandType.Join, ChannelId = channelId };
    }
    public ParsedCommand renameCommandArgParser(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return new ParsedCommand { Type = CommandType.Invalid };
        // Splint string into /rename displayName
        var parts = arg.Split(' ', 2);
        if (parts == null || parts.Length != 2) 
            return new ParsedCommand { Type = CommandType.Invalid };
        // Check if displayName is valid
        var displayName = MatchRegex(parts[1], @"^[\x21-\x7E]{1,20}$");
        if (displayName == null) 
            return new ParsedCommand { Type = CommandType.Invalid };

        return new ParsedCommand { Type = CommandType.Rename, DisplayName = displayName };
    }
    public static string? MatchRegex(string message, string pattern)
    {
        var match = Regex.Match(message, pattern);
        return match.Success ? match.Value : null;
    }
}
