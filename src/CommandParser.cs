public class CommandParser
{
    public enum CommandType { Auth, Msg, Join, Rename, Bye, Help, Error, Invalid }

    public class ParsedCommand
    {
        public CommandType Type;
        public string? Payload;
        public string? Username;
        public string? DisplayName;
        public string? Secret;
        public string? ChannelId;
    }

    public ParsedCommand Parse(string input)
    {
        if (!input.StartsWith("/")) return new ParsedCommand { Type = CommandType.Msg, Payload = input };

        if (input.StartsWith("/auth"))
        {
            return authCommandArgParser(input);
        }
        else if (input.StartsWith("/join"))
        {
            return joinCommandArgParser(input);
        }
        else if (input.StartsWith("/rename"))
        {
            return renameCommandArgParser(input);
        }
        else if (input.StartsWith("/bye"))
        {
            return new ParsedCommand { Type = CommandType.Bye };
        }
        else if (input.StartsWith("/help"))
        {
            return new ParsedCommand { Type = CommandType.Help };
        }
        else if (input.StartsWith("/error"))
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

        var username = parts[1];
        var secret = parts[2];
        var displayName = parts[3];

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

        var channelId = parts[1];

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

        var displayName = parts[1];

        return new ParsedCommand { Type = CommandType.Rename, DisplayName = displayName };
    }
}
