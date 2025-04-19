public class ClientMessageHandler
{
    public enum CommandType { ReplyOK, ReplyNOK, Bye, Err, Msg, Malformed }

    public class ParsedMessage
    {
        public CommandType Type;
        public string? Content;
        public string? DisplayName;
    }
    public ParsedMessage HandleMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debugger.PrintWarning("Received empty message.");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        if (message.ToLower().StartsWith("bye"))
        {
            return HandleBye(message);
        }
        else if (message.ToLower().StartsWith("err"))
        {
            return HandleErr(message);
        
        }
        else if (message.ToLower().StartsWith("reply"))
        {
            return HandleReply(message);
        }
        else if (message.ToLower().StartsWith("msg"))
        {
            return HandleMsg(message);
        }
        else
        {
            Debugger.PrintWarning($"Received malformed message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
    }

    private static ParsedMessage HandleBye(string message)
    {
        // BYE FROM <displayName>
        var parts = message.Split(' ', 3);
        if (parts.Length != 3)
        {
            Debugger.PrintWarning($"Malformed BYE message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        if (parts[1].ToLower() != "from")
        {
            Debugger.PrintWarning($"Malformed BYE message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var displayName = CommandParser.MatchRegex(parts[2], @"^[\x21-\x7E]{1,20}$");
        if (displayName == null)
        {
            Debugger.PrintWarning($"Malformed BYE message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        return new ParsedMessage { Type = CommandType.Bye, DisplayName = displayName };
    }
    private static ParsedMessage HandleErr(string message)
    {
        // ERR FROM <displayName> IS <content>
        var parts = message.Split(' ', 5);
        if (parts.Length != 5)
        {
            Debugger.PrintWarning($"Malformed ERR message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        if (parts[1].ToLower() != "from" || parts[3].ToLower() != "is")
        {
            Debugger.PrintWarning($"Malformed ERR message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var displayName = CommandParser.MatchRegex(parts[2], @"^[\x21-\x7E]{1,20}$");
        if (displayName == null)
        {
            Debugger.PrintWarning($"Malformed ERR message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var content = CommandParser.MatchRegex(parts[4], @"^[\x0A\x20-\x7E]{1,60000}$");
        if (content == null)
        {
            Debugger.PrintWarning($"Malformed ERR message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        return new ParsedMessage { Type = CommandType.Err, DisplayName = displayName, Content = content };
    }
    private static ParsedMessage HandleReply(string message)
    {
        // REPLY <OK|NOK> IS <content>
        var parts = message.Split(' ', 4);
        if (parts.Length != 4)
        {
            Debugger.PrintWarning($"Malformed REPLY message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        if (parts[2].ToLower() != "is")
        {
            Debugger.PrintWarning($"Malformed REPLY message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var replyType = parts[1].ToUpper();
        if (replyType != "OK" && replyType != "NOK")
        {
            Debugger.PrintWarning($"Malformed REPLY message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var content = CommandParser.MatchRegex(parts[3], @"^[\x0A\x20-\x7E]{1,60000}$");
        if (content == null)
        {
            Debugger.PrintWarning($"Malformed REPLY message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        return new ParsedMessage { Type = replyType == "OK" ? CommandType.ReplyOK : CommandType.ReplyNOK, Content = content };
    }
    private static ParsedMessage HandleMsg(string message)
    {
        // MSG FROM <displayName> IS <content>
        var parts = message.Split(' ', 5);
        if (parts.Length != 5)
        {
            Debugger.PrintWarning($"Malformed MSG message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        if (parts[1].ToLower() != "from" || parts[3].ToLower() != "is")
        {
            Debugger.PrintWarning($"Malformed MSG message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var displayName = CommandParser.MatchRegex(parts[2], @"^[\x21-\x7E]{1,20}$");
        if (displayName == null)
        {
            Debugger.PrintWarning($"Malformed MSG message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        var content = CommandParser.MatchRegex(parts[4], @"^[\x0A\x20-\x7E]{1,60000}$");
        if (content == null)
        {
            Debugger.PrintWarning($"Malformed MSG message: {message}");
            return new ParsedMessage { Type = CommandType.Malformed };
        }
        return new ParsedMessage { Type = CommandType.Msg, DisplayName = displayName, Content = content };
    }
}