#nullable enable
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Text.Json;
using QuickCode.Cli.Configuration;
using QuickCode.Cli.Models;
using QuickCode.Cli.Services;
using QuickCode.Cli.Utilities;

namespace QuickCode.Cli;

public sealed class CliApplication
{
    private readonly ConfigService _configService = new();

    public Task<int> RunAsync(string[] args)
    {
        var root = BuildRootCommand();
        return root.InvokeAsync(args);
    }

    private RootCommand BuildRootCommand()
    {
        var verboseOption = new Option<bool>("--verbose", "Show HTTP request/response logs.");
        var versionOption = new Option<bool>("--version", "Show version information.");
        
        var root = new RootCommand("QuickCode API CLI") { verboseOption, versionOption };
        
        root.SetHandler((bool verbose, bool version) =>
        {
            if (version)
            {
                var versionInfo = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                 ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                                 ?? "Unknown";
                Console.WriteLine($"quickcode version {versionInfo}");
                return;
            }
            
            // If no command provided, show help
            root.Invoke("--help");
        }, verboseOption, versionOption);

        root.AddCommand(BuildConfigCommand(verboseOption));
        root.AddCommand(BuildProjectCommand(verboseOption));
        root.AddCommand(BuildModuleCommand(verboseOption));
        root.AddCommand(BuildGenerateCommand(verboseOption));
        root.AddCommand(BuildStatusCommand(verboseOption));

        return root;
    }

    private Command BuildConfigCommand(Option<bool> verboseOption)
    {
        var projectOption = new Option<string?>("--project", "Project name for project-specific config.");
        var setOption = new Option<string[]>("--set", description: "Set configuration values (key=value). Use with --project for project-specific settings.")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var getOption = new Option<string[]>("--get", description: "Get configuration values (key). Use with --project for project-specific settings.")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var unsetOption = new Option<string[]>("--unset", description: "Unset configuration keys. Use with --project for project-specific settings.")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("config", "Manage CLI configuration")
        {
            projectOption,
            setOption,
            getOption,
            unsetOption
        };

        command.SetHandler((string? project, string[] setValues, string[] getValues, string[] unsetValues) =>
        {
            var config = _configService.Load();

            if (setValues.Length > 0)
            {
                foreach (var entry in setValues)
                {
                    var parts = entry.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                    {
                        Console.WriteLine($"Invalid set syntax: {entry}");
                        continue;
                    }
                    ApplySet(config, project, parts[0], parts[1]);
                }
                _configService.Save(config);
            }

            if (unsetValues.Length > 0)
            {
                foreach (var key in unsetValues)
                {
                    ApplyUnset(config, project, key);
                }
                _configService.Save(config);
            }

            if (getValues.Length > 0)
            {
                foreach (var key in getValues)
                {
                    Console.WriteLine($"{key} = {ResolveValue(config, project, key) ?? "<null>"}");
                }
                return;
            }

            if (setValues.Length == 0 && unsetValues.Length == 0 && getValues.Length == 0)
            {
                PrintConfig(config);
            }
        }, projectOption, setOption, getOption, unsetOption);

        return command;
    }

    private Command BuildProjectCommand(Option<bool> verboseOption)
    {
        var project = new Command("project", "Manage QuickCode projects");

        project.AddCommand(BuildProjectCreateCommand(verboseOption));
        project.AddCommand(BuildProjectCheckCommand(verboseOption));
        project.AddCommand(BuildProjectForgotSecretCommand(verboseOption));
        project.AddCommand(BuildProjectVerifySecretCommand(verboseOption));

        return project;
    }

    private Command BuildProjectCreateCommand(Option<bool> verboseOption)
    {
        var command = new Command("create", "Create project or request secret code email");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string>("--email") { IsRequired = true };

        command.AddOption(nameOption);
        command.AddOption(emailOption);

        command.SetHandler(async (string name, string email, bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var result = await client.CreateProjectAsync(name, email);
            Console.WriteLine(result
                ? $"✅ Project '{name}' created/request submitted. Check email for secret code."
                : "⚠️ Project creation request failed.");
        }, nameOption, emailOption, verboseOption);

        return command;
    }

    private Command BuildProjectVerifySecretCommand(Option<bool> verboseOption)
    {
        var command = new Command("verify-secret", "Verify project email and secret code combination");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string>("--email") { IsRequired = true };
        var secretOption = new Option<string>("--secret-code") { IsRequired = true };

        command.AddOption(nameOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string name, string email, string secret, bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var isValid = await client.CheckSecretCodeAsync(name, email, secret);
            Console.WriteLine(isValid
                ? "✅ Secret code is valid."
                : "❌ Secret code is invalid.");
        }, nameOption, emailOption, secretOption, verboseOption);

        return command;
    }

    private Command BuildProjectCheckCommand(Option<bool> verboseOption)
    {
        var command = new Command("check", "Check if a project exists");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        command.AddOption(nameOption);

        command.SetHandler(async (string name, bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var exists = await client.CheckProjectNameAsync(name);
            Console.WriteLine(exists
                ? $"✅ Project '{name}' exists."
                : $"❌ Project '{name}' not found.");
        }, nameOption, verboseOption);

        return command;
    }

    private Command BuildProjectForgotSecretCommand(Option<bool> verboseOption)
    {
        var command = new Command("forgot-secret", "Request secret code reminder email");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string>("--email") { IsRequired = true };

        command.AddOption(nameOption);
        command.AddOption(emailOption);

        command.SetHandler(async (string name, string email, bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var result = await client.ForgotSecretCodeAsync(name, email);
            Console.WriteLine(result
                ? "✅ Secret code reminder sent."
                : "⚠️ Could not send secret code reminder.");
        }, nameOption, emailOption, verboseOption);

        return command;
    }

    private Command BuildModuleCommand(Option<bool> verboseOption)
    {
        var command = new Command("module", "Manage project modules");

        command.AddCommand(BuildModuleAvailableCommand(verboseOption));
        command.AddCommand(BuildModuleListCommand(verboseOption));
        command.AddCommand(BuildModuleAddCommand(verboseOption));
        command.AddCommand(BuildModuleRemoveCommand(verboseOption));
        command.AddCommand(BuildModuleGetDbmlCommand(verboseOption));
        command.AddCommand(BuildModuleSaveDbmlCommand(verboseOption));

        return command;
    }

    private Command BuildModuleAvailableCommand(Option<bool> verboseOption)
    {
        var command = new Command("available", "List available module templates");
        command.SetHandler(async (bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var modules = await client.GetAvailableModulesAsync();
            Console.WriteLine(modules.ToString());
        }, verboseOption);
        return command;
    }

    private Command BuildModuleListCommand(Option<bool> verboseOption)
    {
        var command = new Command("list", "List modules for a project");
        var projectOption = new Option<string?>("--project");

        command.AddOption(projectOption);

        command.SetHandler(async (string? projectName, bool verbose) =>
        {
            var config = _configService.Load();
            var (name, _, _) = _configService.ResolveProjectCredentials(config, projectName, null, null);
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var modules = await client.GetProjectModulesAsync(name);
            CliHelpers.RenderModuleList(modules);
        }, projectOption, verboseOption);

        return command;
    }

    private Command BuildModuleAddCommand(Option<bool> verboseOption)
    {
        var command = new Command("add", "Add module to project");
        var projectOption = new Option<string?>("--project");
        var emailOption = new Option<string?>("--email");
        var secretOption = new Option<string?>("--secret-code");
        var moduleNameOption = new Option<string>("--module-name") { IsRequired = true };
        var templateOption = new Option<string>("--template-key") { IsRequired = true };
        var dbTypeOption = new Option<string>("--db-type", () => "MsSql");
        var patternOption = new Option<string>("--pattern", () => "Service");

        command.AddOption(projectOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);
        command.AddOption(moduleNameOption);
        command.AddOption(templateOption);
        command.AddOption(dbTypeOption);
        command.AddOption(patternOption);

        command.SetHandler(async (string? projectName, string? email, string? secret, string moduleName,
            string templateKey, string dbType, string pattern, bool verbose) =>
        {
            var config = _configService.Load();
            var (name, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, projectName, email, secret);
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var result = await client.AddProjectModuleAsync(name, resolvedEmail, resolvedSecret, moduleName, templateKey, dbType, pattern);
            Console.WriteLine(result ? "✅ Module added." : "⚠️ Module add failed.");
        }, projectOption, emailOption, secretOption, moduleNameOption, templateOption, dbTypeOption, patternOption, verboseOption);

        return command;
    }

    private Command BuildModuleRemoveCommand(Option<bool> verboseOption)
    {
        var command = new Command("remove", "Remove module from project");
        var projectOption = new Option<string?>("--project");
        var emailOption = new Option<string?>("--email");
        var secretOption = new Option<string?>("--secret-code");
        var moduleNameOption = new Option<string>("--module-name") { IsRequired = true };

        command.AddOption(projectOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);
        command.AddOption(moduleNameOption);

        command.SetHandler(async (string? projectName, string? email, string? secret, string moduleName, bool verbose) =>
        {
            var config = _configService.Load();
            var (name, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, projectName, email, secret);
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var result = await client.RemoveProjectModuleAsync(name, resolvedEmail, resolvedSecret, moduleName);
            Console.WriteLine(result ? "✅ Module removed." : "⚠️ Module removal failed.");
        }, projectOption, emailOption, secretOption, moduleNameOption, verboseOption);

        return command;
    }

    private Command BuildModuleGetDbmlCommand(Option<bool> verboseOption)
    {
        var command = new Command("get-dbml", "Download module DBML");
        var projectOption = new Option<string>("--project") { IsRequired = true };
        var moduleNameOption = new Option<string>("--module-name") { IsRequired = true };
        var templateOption = new Option<string>("--template-key") { IsRequired = true };
        var emailOption = new Option<string>("--email") { IsRequired = true };
        var secretOption = new Option<string>("--secret-code") { IsRequired = true };
        var outputOption = new Option<FileInfo?>("--output", "File to save DBML.");

        command.AddOption(projectOption);
        command.AddOption(moduleNameOption);
        command.AddOption(templateOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);
        command.AddOption(outputOption);

        command.SetHandler(async (string project, string moduleName, string templateKey, string projectEmail,
            string secretCode, FileInfo? output, bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var dbml = await client.GetModuleDbmlAsync(project, moduleName, templateKey, projectEmail, secretCode);
            if (output is not null)
            {
                await File.WriteAllTextAsync(output.FullName, dbml);
                Console.WriteLine($"✅ DBML saved to {output.FullName}");
            }
            else
            {
                Console.WriteLine(dbml);
            }
        }, projectOption, moduleNameOption, templateOption, emailOption, secretOption, outputOption, verboseOption);

        return command;
    }

    private Command BuildModuleSaveDbmlCommand(Option<bool> verboseOption)
    {
        var command = new Command("save-dbml", "Upload module DBML");
        var projectOption = new Option<string?>("--project");
        var emailOption = new Option<string?>("--email");
        var secretOption = new Option<string?>("--secret-code");
        var moduleNameOption = new Option<string>("--module-name") { IsRequired = true };
        var templateOption = new Option<string>("--template-key") { IsRequired = true };
        var fileOption = new Option<FileInfo?>("--file", "Path to DBML file.");
        var dbmlOption = new Option<string?>("--dbml", "Inline DBML content.");
        var dbTypeOption = new Option<string>("--db-type", () => "MsSql");

        command.AddOption(projectOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);
        command.AddOption(moduleNameOption);
        command.AddOption(templateOption);
        command.AddOption(fileOption);
        command.AddOption(dbmlOption);
        command.AddOption(dbTypeOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var projectName = ctx.ParseResult.GetValueForOption(projectOption);
            var email = ctx.ParseResult.GetValueForOption(emailOption);
            var secret = ctx.ParseResult.GetValueForOption(secretOption);
            var moduleName = ctx.ParseResult.GetValueForOption(moduleNameOption)!;
            var templateKey = ctx.ParseResult.GetValueForOption(templateOption)!;
            var file = ctx.ParseResult.GetValueForOption(fileOption);
            var dbmlInline = ctx.ParseResult.GetValueForOption(dbmlOption);
            var dbType = ctx.ParseResult.GetValueForOption(dbTypeOption) ?? "MsSql";
            var verbose = ctx.ParseResult.GetValueForOption(verboseOption);

            var config = _configService.Load();
            var (name, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, projectName, email, secret);

            var dbmlContent = dbmlInline;
            if (dbmlContent is null && file is not null)
            {
                dbmlContent = await File.ReadAllTextAsync(file.FullName);
            }

            if (string.IsNullOrWhiteSpace(dbmlContent))
            {
                throw new InvalidOperationException("Provide DBML content via --dbml or --file.");
            }

            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var result = await client.SaveModuleDbmlAsync(name, resolvedEmail, resolvedSecret, moduleName, templateKey, dbmlContent, dbType);
            Console.WriteLine(result ? "✅ DBML saved." : "⚠️ DBML save failed.");
        });

        return command;
    }

    private Command BuildGenerateCommand(Option<bool> verboseOption)
    {
        var command = new Command("generate", "Trigger project generation");
        var projectArg = new Argument<string?>("project", "Project name (optional if default configured).");
        var emailOption = new Option<string?>("--email", "Override project email.");
        var secretOption = new Option<string?>("--secret-code", "Override secret code.");
        var sessionOption = new Option<string?>("--session-id", "Custom session id.");
        var watchOption = new Option<bool>("--watch", "Watch generation progress.");

        command.AddArgument(projectArg);
        command.AddOption(emailOption);
        command.AddOption(secretOption);
        command.AddOption(sessionOption);
        command.AddOption(watchOption);

        command.SetHandler(async (string? project, string? email, string? secret, string? sessionId, bool watch, bool verbose) =>
        {
            var config = _configService.Load();
            var (name, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, project, email, secret);
            var session = string.IsNullOrWhiteSpace(sessionId) ? CliHelpers.GenerateSessionId() : sessionId!;

            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var success = await client.GenerateProjectSolutionAsync(name, resolvedEmail, resolvedSecret, session);
            if (!success)
            {
                Console.WriteLine("⚠️ API returned failure response.");
                return;
            }

            Console.WriteLine($"✅ Generation started for '{name}'. Session: {session}");
            if (watch)
            {
                await WatchGenerationAsync(config, client, session, verbose);
            }
        }, projectArg, emailOption, secretOption, sessionOption, watchOption, verboseOption);

        return command;
    }

    private Command BuildStatusCommand(Option<bool> verboseOption)
    {
        var command = new Command("status", "Check generation status with session id");
        var sessionOption = new Option<string>("--session-id") { IsRequired = true };
        command.AddOption(sessionOption);

        command.SetHandler(async (string sessionId, bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var status = await client.GetActiveProjectAsync(sessionId);
            if (status is null)
            {
                Console.WriteLine("No active generation found.");
                return;
            }

            Console.WriteLine($"Run ID: {status.ActiveRunId}");
            Console.WriteLine($"Project: {status.ProjectName}");
            Console.WriteLine($"Started: {status.StartDate}");
            Console.WriteLine($"Finished: {status.IsFinished}");
        }, sessionOption, verboseOption);

        return command;
    }

    private void ApplySet(QuickCodeConfig config, string? project, string key, string value)
    {
        key = key.Trim();
        if (string.IsNullOrWhiteSpace(project))
        {
            if (key is "api_url")
            {
                config.ApiUrl = value;
            }
            else
            {
                Console.WriteLine("Global config only supports 'api_url'. Use --project for project-specific keys.");
            }
            return;
        }

        var projectConfig = config.Projects.TryGetValue(project, out var existing)
            ? existing
            : new ProjectConfig();

        switch (key)
        {
            case "email":
                projectConfig.Email = value;
                break;
            case "secret_code":
                projectConfig.SecretCode = value;
                break;
            default:
                Console.WriteLine($"Unknown project config key: {key}");
                return;
        }

        config.Projects[project] = projectConfig;
    }

    private void ApplyUnset(QuickCodeConfig config, string? project, string key)
    {
        if (string.IsNullOrWhiteSpace(project))
        {
            if (key is "api_url")
            {
                config.ApiUrl = "https://api.quickcode.net/";
            }
            else
            {
                Console.WriteLine("Global config only supports 'api_url'. Use --project for project-specific keys.");
            }
            return;
        }

        if (!config.Projects.TryGetValue(project, out var projectConfig))
        {
            return;
        }

        switch (key)
        {
            case "email":
                projectConfig.Email = null;
                break;
            case "secret_code":
                projectConfig.SecretCode = null;
                break;
            default:
                Console.WriteLine($"Unknown project config key: {key}");
                break;
        }

        if (string.IsNullOrWhiteSpace(projectConfig.Email) && string.IsNullOrWhiteSpace(projectConfig.SecretCode))
        {
            config.Projects.Remove(project);
        }
    }

    private static string? ResolveValue(QuickCodeConfig config, string? project, string key)
    {
        if (string.IsNullOrWhiteSpace(project))
        {
            return key switch
            {
                "api_url" => config.ApiUrl,
                _ => null
            };
        }

        if (!config.Projects.TryGetValue(project, out var projectConfig))
        {
            return null;
        }

        return key switch
        {
            "email" => projectConfig.Email,
            "secret_code" => projectConfig.SecretCode,
            _ => null
        };
    }

    private static void PrintConfig(QuickCodeConfig config)
    {
        Console.WriteLine("api_url = " + config.ApiUrl);

        if (config.Projects.Count > 0)
        {
            Console.WriteLine("projects:");
            foreach (var (name, project) in config.Projects)
            {
                Console.WriteLine($"  [{name}] email={project.Email ?? "<null>"} secret_code={(project.SecretCode is null ? "<null>" : "********")}");
            }
        }
    }

    private async Task WatchGenerationAsync(QuickCodeConfig config, QuickCodeApiClient client, string sessionId, bool verbose)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, args) =>
        {
            args.Cancel = true;
            cts.Cancel();
        };

        CancellationTokenSource? verifyCts = null;

        try
        {
            await using var watcher = new GenerationWatcher(config.ApiUrl, sessionId, verbose);
            watcher.OnUpdate += evt =>
            {
                verifyCts?.Cancel();
                verifyCts?.Dispose();
                verifyCts = new CancellationTokenSource();

                CliHelpers.RenderStepProgress(evt.AllSteps, evt.AllActions);
                if (CliHelpers.AreAllActionsCompleted(evt.AllActions))
                {
                    Console.WriteLine("✅ Generation completed. Exiting watcher...");
                    cts.Cancel();
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), verifyCts.Token);
                        verifyCts.Token.ThrowIfCancellationRequested();

                        Console.WriteLine("ℹ️ No SignalR updates for 5s, verifying status via HTTP...");
                        var status = await client.GetActiveProjectAsync(sessionId);
                        if (status is not null)
                        {
                            RenderPollingStatus(status);
                            
                            // Check if all actions are completed
                            try
                            {
                                var stepsData = await client.GetGenerationStepsAsync();
                                if (stepsData.ValueKind == JsonValueKind.Object)
                                {
                                    // Try to extract allActions from the response
                                    if (stepsData.TryGetProperty("allActions", out var allActions) ||
                                        stepsData.TryGetProperty("actions", out allActions))
                                    {
                                        if (CliHelpers.AreAllActionsCompleted(allActions))
                                        {
                                            Console.WriteLine("✅ All actions completed (HTTP verification). Exiting watcher...");
                                            cts.Cancel();
                                            return;
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // If we can't get actions, fall back to IsFinished check
                            }
                            
                            // Fallback to IsFinished check if action check fails
                            if (status.IsFinished)
                            {
                                Console.WriteLine("✅ Generation completed (HTTP verification). Exiting watcher...");
                                cts.Cancel();
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // normal when new update arrives
                    }
                    catch (Exception httpEx)
                    {
                        Console.WriteLine($"⚠️ HTTP status check failed: {httpEx.Message}");
                    }
                }, CancellationToken.None);
            };

            await watcher.RunAsync(cts.Token);
            Console.WriteLine("Watcher stopped.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Watcher cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR error: {ex.Message}");
            Console.WriteLine("Falling back to HTTP polling...");

            Console.WriteLine($"SignalR error: {ex.Message}");
            Console.WriteLine("Falling back to HTTP polling...");

            var polling = new GenerationPollingService(client);
            await polling.RunAsync(sessionId, TimeSpan.FromSeconds(2), async response =>
            {
                RenderPollingStatus(response);
                
                // Check if all actions are completed
                try
                {
                    var stepsData = await client.GetGenerationStepsAsync();
                    if (stepsData.ValueKind == JsonValueKind.Object)
                    {
                        // Try to extract allActions from the response
                        if (stepsData.TryGetProperty("allActions", out var allActions) ||
                            stepsData.TryGetProperty("actions", out allActions))
                        {
                            if (CliHelpers.AreAllActionsCompleted(allActions))
                            {
                                Console.WriteLine("✅ All actions completed. Exiting watcher...");
                                cts.Cancel();
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    // If we can't get actions, fall back to IsFinished check
                }
                
                // Fallback to IsFinished check if action check fails
                if (response.IsFinished)
                {
                    Console.WriteLine("✅ Generation completed. Exiting watcher...");
                    cts.Cancel();
                }
            }, cts.Token);
        }
        finally
        {
            verifyCts?.Cancel();
            verifyCts?.Dispose();
        }
    }

    private static void RenderPollingStatus(ActiveProjectResponse response)
    {
        Console.WriteLine($"Run ID: {response.ActiveRunId} | Project: {response.ProjectName} | Finished: {response.IsFinished}");
    }
}

