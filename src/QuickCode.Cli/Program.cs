using QuickCode.Cli;
using QuickCode.Cli.Utilities;

var normalizedArgs = ArgumentNormalizer.Normalize(args);
var app = new CliApplication();
return await app.RunAsync(normalizedArgs);
