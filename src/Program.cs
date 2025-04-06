class Program
{
    static void Main(string[] args)
    {
        try
        {
            ArgParser.ParseArguments(args);
        }
        catch (Exception ex)
        {
            Debugger.PrintError(ex.Message);
            Environment.Exit(1);   
        }
        
        if (Debugger.DebugMode)
        {
            Debugger.PrintInfo();
        }
        
    }
}
