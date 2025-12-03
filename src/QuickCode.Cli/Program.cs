using QuickCode.Cli;
using QuickCode.Cli.Utilities;

try
{
    var normalizedArgs = ArgumentNormalizer.Normalize(args);
    var app = new CliApplication();
    return await app.RunAsync(normalizedArgs);
}
catch (InvalidOperationException ex)
{
    // User-friendly error messages for configuration issues
    // Message already contains emoji and formatting from ConfigService
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex.Message);
    Console.ResetColor();
    return 1;
}
catch (HttpRequestException ex)
{
    // User-friendly error messages for HTTP errors
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex.Message);
    Console.ResetColor();
    return 1;
}
catch (Exception ex)
{
    // Check if verbose flag is set
    var isVerbose = args.Contains("--verbose", StringComparer.OrdinalIgnoreCase) || 
                    args.Contains("-v", StringComparer.OrdinalIgnoreCase);
    
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ An error occurred: {ex.Message}");
    Console.ResetColor();
    
    if (isVerbose)
    {
        Console.WriteLine();
        Console.WriteLine("Stack trace:");
        Console.WriteLine(ex.StackTrace);
    }
    else
    {
        Console.WriteLine("💡 Run with --verbose for more details.");
    }
    
    return 1;
}
