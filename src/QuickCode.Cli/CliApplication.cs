#nullable enable
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using QuickCode.Cli.Configuration;
using QuickCode.Cli.Models;
using QuickCode.Cli.Services;
using QuickCode.Cli.Utilities;

namespace QuickCode.Cli;

public sealed class CliApplication
{
    private static readonly Uri ProjectReadmeUri = new("https://raw.githubusercontent.com/QuickCodeNet/quickcode.cli/main/README.md");
    private readonly ConfigService _configService = new();

    public Task<int> RunAsync(string[] args)
    {
        var root = BuildRootCommand();
        return root.InvokeAsync(args);
    }

    private RootCommand BuildRootCommand()
    {
        var verboseOption = new Option<bool>("--verbose", "Show HTTP request/response logs.");
        
        var root = new RootCommand("QuickCode API CLI") { verboseOption };

        root.AddCommand(BuildConfigCommand(verboseOption));
        root.AddCommand(BuildProjectCommand(verboseOption));
        root.AddCommand(BuildModuleCommand(verboseOption));
        root.AddCommand(BuildCreateRootCommand(verboseOption));
        root.AddCommand(BuildCheckRootCommand(verboseOption));
        root.AddCommand(BuildForgotSecretRootCommand(verboseOption));
        root.AddCommand(BuildVerifySecretRootCommand(verboseOption));
        root.AddCommand(BuildGetDbmlsRootCommand(verboseOption));
        root.AddCommand(BuildUpdateDbmlsRootCommand(verboseOption));
        root.AddCommand(BuildValidateRootCommand(verboseOption));
        root.AddCommand(BuildRemoveRootCommand(verboseOption));
        root.AddCommand(BuildTemplatesRootCommand(verboseOption));
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

        var validateCommand = new Command("validate", "Validate project configurations");
        
        var command = new Command(
            "config",
            """
            Manage CLI configuration.
            
            Subcommands:
              validate          Validate all project credentials.
            
            Options:
              --set             key=value pairs (global or --project scoped).
              --project         Apply changes to a specific project.
            """)
        {
            projectOption,
            setOption,
            getOption,
            unsetOption
        };
        
        command.AddCommand(validateCommand);
        
        validateCommand.SetHandler(() =>
        {
            var config = _configService.Load();
            ValidateConfig(config);
        });

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
        var project = new Command(
            "project",
            """
            Manage QuickCode projects.
            
            Subcommands:
              create          Create project / request secret email.
              check           Check if a project exists.
              forgot-secret   Send secret reminder email.
              verify-secret   Verify email + secret combination.
              get-dbmls       Download project & template DBML files.
              update-dbmls    Upload DBML files back to the API.
              remove          Remove stored credentials and DBML folder.
              validate        Validate stored project credentials.
            """);

        project.AddCommand(BuildProjectCreateCommand(verboseOption));
        project.AddCommand(BuildProjectCheckCommand(verboseOption));
        project.AddCommand(BuildProjectForgotSecretCommand(verboseOption));
        project.AddCommand(BuildProjectVerifySecretCommand(verboseOption));
        project.AddCommand(BuildProjectValidateCommand(verboseOption));
        project.AddCommand(BuildProjectDownloadDbmlsCommand(verboseOption));
        project.AddCommand(BuildProjectUpdateDbmlsCommand(verboseOption));
        project.AddCommand(BuildProjectRemoveCommand(verboseOption));

        return project;
    }

    private Command BuildProjectRemoveCommand(Option<bool> verboseOption)
    {
        var command = new Command("remove", "Remove stored project credentials and local DBML folder");
        var nameOption = new Option<string>("--name") { IsRequired = true };

        command.AddOption(nameOption);

        command.SetHandler((string name, bool verbose) =>
        {
            HandleProjectRemove(name);
        }, nameOption, verboseOption);

        return command;
    }

    private Command BuildCreateRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var command = new Command("create", "Shortcut for 'project create'.")
        {
            projectArg
        };

        var emailOption = new Option<string>("--email", "Project email") { IsRequired = true };
        command.AddOption(emailOption);

        command.SetHandler(async (string project, string email, bool verbose) =>
        {
            await HandleProjectCreateAsync(project, email, verbose);
        }, projectArg, emailOption, verboseOption);

        return command;
    }

    private Command BuildCheckRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var command = new Command("check", "Shortcut for 'project check'.")
        {
            projectArg
        };

        command.SetHandler(async (string project, bool verbose) =>
        {
            await HandleProjectCheckAsync(project, verbose);
        }, projectArg, verboseOption);

        return command;
    }

    private Command BuildForgotSecretRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var emailOption = new Option<string?>("--email", "Override stored project email.");

        var command = new Command("forgot-secret", "Shortcut for 'project forgot-secret'.")
        {
            projectArg
        };
        command.AddOption(emailOption);

        command.SetHandler(async (string project, string? email, bool verbose) =>
        {
            await HandleProjectForgotSecretAsync(project, email, verbose);
        }, projectArg, emailOption, verboseOption);

        return command;
    }

    private Command BuildVerifySecretRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var emailOption = new Option<string?>("--email", "Override stored project email.");
        var secretOption = new Option<string?>("--secret-code", "Override stored project secret code.");

        var command = new Command("verify-secret", "Shortcut for 'project verify-secret'.")
        {
            projectArg
        };

        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string project, string? email, string? secret, bool verbose) =>
        {
            await HandleProjectVerifySecretAsync(project, email, secret, verbose);
        }, projectArg, emailOption, secretOption, verboseOption);

        return command;
    }

    private Command BuildGetDbmlsRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var emailOption = new Option<string?>("--email", "Override project email.");
        var secretOption = new Option<string?>("--secret-code", "Override project secret code.");

        var command = new Command("get-dbmls", "Shortcut for 'project get-dbmls'.")
        {
            projectArg
        };
        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string project, string? email, string? secret, bool verbose) =>
        {
            await HandleProjectDownloadDbmlsAsync(project, email, secret, verbose);
        }, projectArg, emailOption, secretOption, verboseOption);

        return command;
    }

    private Command BuildUpdateDbmlsRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var emailOption = new Option<string?>("--email", "Override project email.");
        var secretOption = new Option<string?>("--secret-code", "Override project secret code.");

        var command = new Command("update-dbmls", "Shortcut for 'project update-dbmls'.")
        {
            projectArg
        };
        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string project, string? email, string? secret, bool verbose) =>
        {
            await HandleProjectUpdateDbmlsAsync(project, email, secret, verbose);
        }, projectArg, emailOption, secretOption, verboseOption);

        return command;
    }

    private Command BuildValidateRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var command = new Command("validate", "Shortcut for 'project validate'.")
        {
            projectArg
        };

        command.SetHandler((string project, bool verbose) =>
        {
            var config = _configService.Load();
            ValidateProjectConfig(config, project);
        }, projectArg, verboseOption);

        return command;
    }

    private Command BuildRemoveRootCommand(Option<bool> verboseOption)
    {
        var projectArg = new Argument<string>("project", "Project name.");
        var command = new Command("remove", "Remove stored project credentials and local DBML folder.")
        {
            projectArg
        };

        command.SetHandler((string project, bool verbose) =>
        {
            HandleProjectRemove(project);
        }, projectArg, verboseOption);

        return command;
    }

    private Command BuildTemplatesRootCommand(Option<bool> verboseOption)
    {
        var command = new Command("templates", "List available module templates (shortcut for 'module available').");

        command.SetHandler(async (bool verbose) =>
        {
            var config = _configService.Load();
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var modules = await client.GetAvailableModulesAsync();
            Console.WriteLine(modules.ToString());
        }, verboseOption);

        return command;
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
            await HandleProjectCreateAsync(name, email, verbose);
        }, nameOption, emailOption, verboseOption);

        return command;
    }

    private Command BuildProjectVerifySecretCommand(Option<bool> verboseOption)
    {
        var command = new Command("verify-secret", "Verify project email and secret code combination");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string?>("--email", "Override stored project email.");
        var secretOption = new Option<string?>("--secret-code", "Override stored project secret code.");

        command.AddOption(nameOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string name, string? email, string? secret, bool verbose) =>
        {
            await HandleProjectVerifySecretAsync(name, email, secret, verbose);
        }, nameOption, emailOption, secretOption, verboseOption);

        return command;
    }

    private Command BuildProjectValidateCommand(Option<bool> verboseOption)
    {
        var command = new Command("validate", "Validate project configuration");
        var nameOption = new Option<string>("--name") { IsRequired = true };

        command.AddOption(nameOption);

        command.SetHandler((string name, bool verbose) =>
        {
            var config = _configService.Load();
            ValidateProjectConfig(config, name);
        }, nameOption, verboseOption);

        return command;
    }

    private Command BuildProjectCheckCommand(Option<bool> verboseOption)
    {
        var command = new Command("check", "Check if a project exists");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        command.AddOption(nameOption);

        command.SetHandler(async (string name, bool verbose) =>
        {
            await HandleProjectCheckAsync(name, verbose);
        }, nameOption, verboseOption);

        return command;
    }

    private Command BuildProjectForgotSecretCommand(Option<bool> verboseOption)
    {
        var command = new Command("forgot-secret", "Request secret code reminder email");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string?>("--email", "Override stored project email.");

        command.AddOption(nameOption);
        command.AddOption(emailOption);

        command.SetHandler(async (string name, string? email, bool verbose) =>
        {
            await HandleProjectForgotSecretAsync(name, email, verbose);
        }, nameOption, emailOption, verboseOption);

        return command;
    }

    private Command BuildModuleCommand(Option<bool> verboseOption)
    {
        var command = new Command(
            "module",
            """
            Manage project modules.
            
            Subcommands:
              available     List available module templates.
              list          List modules in a project.
              add           Add module to project.
              remove        Remove module from project.
              get-dbml      Download a single module DBML.
              save-dbml     Upload/save DBML content.
            """);

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
        var watchOption = new Option<bool>("--watch", () => true, "Watch generation progress (defaults to enabled).");

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

    private async Task HandleProjectCreateAsync(string projectName, string email, bool verbose)
    {
        var config = _configService.Load();
        using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
        var result = await client.CreateProjectAsync(projectName, email);
        Console.WriteLine(result
            ? $"✅ Project '{projectName}' created/request submitted. Check email for secret code."
            : "⚠️ Project creation request failed.");
    }

    private async Task HandleProjectCheckAsync(string projectName, bool verbose)
    {
        var config = _configService.Load();
        using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
        var exists = await client.CheckProjectNameAsync(projectName);
        Console.WriteLine(exists
            ? $"✅ Project '{projectName}' exists."
            : $"❌ Project '{projectName}' not found.");
    }

    private async Task HandleProjectForgotSecretAsync(string projectName, string? email, bool verbose)
    {
        var config = _configService.Load();
        var resolvedEmail = email;

        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            if (config.Projects.TryGetValue(projectName, out var projectConfig) &&
                !string.IsNullOrWhiteSpace(projectConfig.Email))
            {
                resolvedEmail = projectConfig.Email;
            }
            else
            {
                Console.WriteLine($"❌ Email not configured for '{projectName}'. Set it via 'quickcode {projectName} config --set email=...' or pass --email.");
                return;
            }
        }

        using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
        var result = await client.ForgotSecretCodeAsync(projectName, resolvedEmail);
        Console.WriteLine(result
            ? "✅ Secret code reminder sent."
            : "⚠️ Could not send secret code reminder.");
    }

    private async Task HandleProjectVerifySecretAsync(string projectName, string? email, string? secret, bool verbose)
    {
        var config = _configService.Load();
        try
        {
            var (_, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, projectName, email, secret);
            using var client = new QuickCodeApiClient(config.ApiUrl, verbose);
            var isValid = await client.CheckSecretCodeAsync(projectName, resolvedEmail, resolvedSecret);
            Console.WriteLine(isValid
                ? "✅ Secret code is valid."
                : "❌ Secret code is invalid.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
        }
    }

    private async Task HandleProjectDownloadDbmlsAsync(string projectName, string? email, string? secret, bool verbose)
    {
        var config = _configService.Load();
        var (name, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, projectName, email, secret);

        var currentDir = Directory.GetCurrentDirectory();
        var currentDirName = new DirectoryInfo(currentDir).Name;
        var projectDir = string.Equals(currentDirName, projectName, StringComparison.OrdinalIgnoreCase)
            ? currentDir
            : Path.Combine(currentDir, projectName);
        var templatesDir = Path.Combine(projectDir, "templates");

        if (!Directory.Exists(projectDir))
        {
            Directory.CreateDirectory(projectDir);
            Console.WriteLine($"📁 Created directory: {projectDir}");
        }
        else
        {
            Console.WriteLine($"📁 Using existing directory: {projectDir}");
        }

        if (!Directory.Exists(templatesDir))
        {
            Directory.CreateDirectory(templatesDir);
            Console.WriteLine($"📁 Created templates directory: {templatesDir}");
        }

        await DownloadProjectReadmeAsync(projectDir);

        using var client = new QuickCodeApiClient(config.ApiUrl, verbose);

        Console.WriteLine($"📦 Fetching modules for project '{name}'...");
        var modules = await client.GetProjectModulesAsync(name);

        if (modules.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("❌ Failed to fetch modules or no modules found.");
            return;
        }

        var moduleArray = modules.EnumerateArray().ToList();
        if (moduleArray.Count == 0)
        {
            Console.WriteLine("⚠️ No modules found for this project.");
            return;
        }

        Console.WriteLine($"📦 Found {moduleArray.Count} module(s). Downloading project DBMLs...");
        Console.WriteLine(new string('-', 60));

        var projectSuccessCount = 0;
        var projectFailCount = 0;

        foreach (var module in moduleArray)
        {
            var moduleName = module.TryGetProperty("moduleName", out var nameProp) ? nameProp.GetString() : null;
            var templateKey = module.TryGetProperty("moduleTemplateKey", out var templateProp) ? templateProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(moduleName) || string.IsNullOrWhiteSpace(templateKey))
            {
                Console.WriteLine($"⚠️ Skipping module with missing name or template key.");
                projectFailCount++;
                continue;
            }

            try
            {
                Console.Write($"⬇️  Downloading {moduleName}... ");
                var dbml = await client.GetModuleDbmlAsync(name, moduleName, templateKey, resolvedEmail, resolvedSecret);

                var fileName = $"{moduleName}.dbml";
                var projectFilePath = Path.Combine(projectDir, fileName);

                await File.WriteAllTextAsync(projectFilePath, dbml);

                Console.WriteLine($"✅ Saved to {fileName}");
                projectSuccessCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed: {ex.Message}");
                projectFailCount++;
            }
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"✅ Project modules: {projectSuccessCount} downloaded, {projectFailCount} failed.");

        Console.WriteLine();
        Console.WriteLine("📦 Fetching all template modules...");
        var availableModules = await client.GetAvailableModulesAsync();

        if (availableModules.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("⚠️ Failed to fetch template modules.");
        }
        else
        {
            var templateArray = availableModules.EnumerateArray().ToList();
            if (templateArray.Count > 0)
            {
                Console.WriteLine($"📦 Found {templateArray.Count} template module(s). Downloading to templates folder...");
                Console.WriteLine(new string('-', 60));

                var templateSuccessCount = 0;
                var templateFailCount = 0;

                foreach (var template in templateArray)
                {
                    var templateKey = template.TryGetProperty("key", out var keyProp) ? keyProp.GetString() : null;
                    var templateName = template.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                    if (string.IsNullOrWhiteSpace(templateKey) || string.IsNullOrWhiteSpace(templateName))
                    {
                        Console.WriteLine($"⚠️ Skipping template with missing key or name.");
                        templateFailCount++;
                        continue;
                    }

                    try
                    {
                        Console.Write($"⬇️  Downloading template {templateName}... ");
                        var dbml = await client.GetModuleDbmlAsync(name, templateName, templateKey, resolvedEmail, resolvedSecret);

                        var fileName = $"{templateName}.dbml";
                        var templatesFilePath = Path.Combine(templatesDir, fileName);

                        await File.WriteAllTextAsync(templatesFilePath, dbml);

                        Console.WriteLine($"✅ Saved to templates/{fileName}");
                        templateSuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed: {ex.Message}");
                        templateFailCount++;
                    }
                }

                Console.WriteLine(new string('-', 60));
                Console.WriteLine($"✅ Template modules: {templateSuccessCount} downloaded, {templateFailCount} failed.");
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"📁 Project files saved to: {projectDir}");
        Console.WriteLine($"📁 Template files saved to: {templatesDir}");
    }

    private static async Task DownloadProjectReadmeAsync(string projectDir)
    {
        var readmePath = Path.Combine(projectDir, "README.md");
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var readmeContent = await httpClient.GetStringAsync(ProjectReadmeUri);
            await File.WriteAllTextAsync(readmePath, readmeContent);
            Console.WriteLine($"📄 Saved README.md to {readmePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not download README.md: {ex.Message}");
        }
    }

    private async Task HandleProjectUpdateDbmlsAsync(string projectName, string? email, string? secret, bool verbose)
    {
        var config = _configService.Load();
        var (name, resolvedEmail, resolvedSecret) = _configService.ResolveProjectCredentials(config, projectName, email, secret);

        var currentDir = Directory.GetCurrentDirectory();
        var currentDirName = new DirectoryInfo(currentDir).Name;
        var projectDir = string.Equals(currentDirName, projectName, StringComparison.OrdinalIgnoreCase)
            ? currentDir
            : Path.Combine(currentDir, projectName);

        if (!Directory.Exists(projectDir))
        {
            Console.WriteLine($"❌ Project directory not found: {projectDir}");
            Console.WriteLine($"   Run 'quickcode get-dbmls {projectName}' first to download DBMLs.");
            return;
        }

        var dbmlFiles = Directory.GetFiles(projectDir, "*.dbml", SearchOption.TopDirectoryOnly);

        if (dbmlFiles.Length == 0)
        {
            Console.WriteLine($"⚠️ No DBML files found in {projectDir}");
            return;
        }

        using var client = new QuickCodeApiClient(config.ApiUrl, verbose);

        Console.WriteLine($"📦 Fetching module information for project '{name}'...");
        var modules = await client.GetProjectModulesAsync(name);

        if (modules.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("❌ Failed to fetch modules or no modules found.");
            return;
        }

        var moduleArray = modules.EnumerateArray().ToList();
        var moduleMap = new Dictionary<string, JsonElement>();
        foreach (var module in moduleArray)
        {
            var moduleName = module.TryGetProperty("moduleName", out var nameProp) ? nameProp.GetString() : null;
            if (!string.IsNullOrWhiteSpace(moduleName))
            {
                moduleMap[moduleName] = module;
            }
        }

        Console.WriteLine($"📦 Found {dbmlFiles.Length} DBML file(s). Uploading to API...");
        Console.WriteLine(new string('-', 60));

        var successCount = 0;
        var failCount = 0;

        foreach (var dbmlFile in dbmlFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(dbmlFile);

            if (!moduleMap.TryGetValue(fileName, out var module))
            {
                Console.WriteLine($"⚠️ Skipping {Path.GetFileName(dbmlFile)}: Module '{fileName}' not found in project.");
                failCount++;
                continue;
            }

            var moduleName = module.TryGetProperty("moduleName", out var nameProp) ? nameProp.GetString() : null;
            var templateKey = module.TryGetProperty("moduleTemplateKey", out var templateProp) ? templateProp.GetString() : null;
            var dbTypeKey = module.TryGetProperty("dbTypeKey", out var dbTypeProp) ? dbTypeProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(moduleName) || string.IsNullOrWhiteSpace(templateKey) || string.IsNullOrWhiteSpace(dbTypeKey))
            {
                Console.WriteLine($"⚠️ Skipping {Path.GetFileName(dbmlFile)}: Missing module information.");
                failCount++;
                continue;
            }

            try
            {
                Console.Write($"⬆️  Uploading {Path.GetFileName(dbmlFile)}... ");
                var dbmlContent = await File.ReadAllTextAsync(dbmlFile);

                var result = await client.SaveModuleDbmlAsync(name, resolvedEmail, resolvedSecret, moduleName, templateKey, dbmlContent, dbTypeKey);

                if (result)
                {
                    Console.WriteLine("✅ Uploaded successfully");
                    successCount++;
                }
                else
                {
                    Console.WriteLine("❌ API returned failure");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"✅ Successfully uploaded {successCount} DBML file(s).");
        if (failCount > 0)
        {
            Console.WriteLine($"⚠️  Failed to upload {failCount} file(s).");
        }
    }

    private void HandleProjectRemove(string projectName)
    {
        var config = _configService.Load();
        if (!config.Projects.Remove(projectName))
        {
            Console.WriteLine($"⚠️ Project '{projectName}' not found in config.");
        }
        else
        {
            _configService.Save(config);
            Console.WriteLine($"🗑️ Removed stored credentials for project '{projectName}'.");
        }

        var currentDir = Directory.GetCurrentDirectory();
        var currentDirName = new DirectoryInfo(currentDir).Name;
        var projectDir = string.Equals(currentDirName, projectName, StringComparison.OrdinalIgnoreCase)
            ? currentDir
            : Path.Combine(currentDir, projectName);

        if (Directory.Exists(projectDir))
        {
            try
            {
                Directory.Delete(projectDir, recursive: true);
                Console.WriteLine($"🧹 Deleted DBML folder: {projectDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not delete folder '{projectDir}': {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ No local DBML folder found at {projectDir}");
        }
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
                // Encrypt secret code before saving
                projectConfig.SecretCode = _configService.EncryptSecretValue(value);
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
            "secret_code" => projectConfig.SecretCode != null ? "********" : null, // Never show actual secret
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

    private static void ValidateConfig(QuickCodeConfig config)
    {
        var hasErrors = false;

        if (config.Projects.Count == 0)
        {
            Console.WriteLine("⚠️  No projects configured.");
            return;
        }

        Console.WriteLine("Validating project configurations...");
        Console.WriteLine(new string('-', 60));

        foreach (var (projectName, projectConfig) in config.Projects)
        {
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(projectConfig.Email))
            {
                issues.Add("❌ email is missing");
                hasErrors = true;
            }
            else
            {
                Console.WriteLine($"✅ [{projectName}] email: {projectConfig.Email}");
            }

            if (string.IsNullOrWhiteSpace(projectConfig.SecretCode))
            {
                issues.Add("❌ secret_code is missing");
                hasErrors = true;
            }
            else
            {
                Console.WriteLine($"✅ [{projectName}] secret_code: ********");
            }

            if (issues.Count > 0)
            {
                Console.WriteLine($"⚠️  [{projectName}] has issues:");
                foreach (var issue in issues)
                {
                    Console.WriteLine($"   {issue}");
                }
            }
        }

        Console.WriteLine(new string('-', 60));

        if (hasErrors)
        {
            Console.WriteLine("❌ Validation failed. Some projects are missing required fields.");
            Console.WriteLine("Fix with: quickcode config --project <name> --set email=... secret_code=...");
        }
        else
        {
            Console.WriteLine("✅ All projects are properly configured.");
        }
    }

    private static void ValidateProjectConfig(QuickCodeConfig config, string projectName)
    {
        Console.WriteLine($"Validating project: {projectName}");
        Console.WriteLine(new string('-', 60));

        if (!config.Projects.TryGetValue(projectName, out var projectConfig))
        {
            Console.WriteLine($"❌ Project '{projectName}' is not configured.");
            Console.WriteLine($"Configure with: quickcode config --project {projectName} --set email=... secret_code=...");
            return;
        }

        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(projectConfig.Email))
        {
            issues.Add("❌ email is missing");
        }
        else
        {
            Console.WriteLine($"✅ email: {projectConfig.Email}");
        }

        if (string.IsNullOrWhiteSpace(projectConfig.SecretCode))
        {
            issues.Add("❌ secret_code is missing");
        }
        else
        {
            Console.WriteLine($"✅ secret_code: ********");
        }

        Console.WriteLine(new string('-', 60));

        if (issues.Count > 0)
        {
            Console.WriteLine($"❌ Project '{projectName}' has issues:");
            foreach (var issue in issues)
            {
                Console.WriteLine($"   {issue}");
            }
            Console.WriteLine($"Fix with: quickcode config --project {projectName} --set email=... secret_code=...");
        }
        else
        {
            Console.WriteLine($"✅ Project '{projectName}' is properly configured.");
        }
    }

    private Command BuildProjectDownloadDbmlsCommand(Option<bool> verboseOption)
    {
        var command = new Command("get-dbmls", "Download all module DBMLs to a project folder");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string?>("--email");
        var secretOption = new Option<string?>("--secret-code");

        command.AddOption(nameOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string projectName, string? email, string? secret, bool verbose) =>
        {
            await HandleProjectDownloadDbmlsAsync(projectName, email, secret, verbose);
        }, nameOption, emailOption, secretOption, verboseOption);

        return command;
    }

    private Command BuildProjectUpdateDbmlsCommand(Option<bool> verboseOption)
    {
        var command = new Command("update-dbmls", "Upload all DBML files from project folder to API");
        var nameOption = new Option<string>("--name") { IsRequired = true };
        var emailOption = new Option<string?>("--email");
        var secretOption = new Option<string?>("--secret-code");

        command.AddOption(nameOption);
        command.AddOption(emailOption);
        command.AddOption(secretOption);

        command.SetHandler(async (string projectName, string? email, string? secret, bool verbose) =>
        {
            await HandleProjectUpdateDbmlsAsync(projectName, email, secret, verbose);
        }, nameOption, emailOption, secretOption, verboseOption);

        return command;
    }

    private async Task WatchGenerationAsync(QuickCodeConfig config, QuickCodeApiClient client, string sessionId, bool verbose)
    {
        using var cts = new CancellationTokenSource();
        CliHelpers.ResetProgressArea();
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
                    CliHelpers.ResetProgressArea();
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
                            
                            // If Run ID is -1, the generation session is invalid - exit
                            if (status.ActiveRunId == -1)
                            {
                                CliHelpers.ResetProgressArea();
                                Console.WriteLine("❌ Invalid generation session (Run ID: -1). Exiting watcher...");
                                cts.Cancel();
                                return;
                            }
                            
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
                                CliHelpers.ResetProgressArea();
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
                                CliHelpers.ResetProgressArea();
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
                
                // If Run ID is -1, the generation session is invalid - exit
                if (response.ActiveRunId == -1)
                {
                    CliHelpers.ResetProgressArea();
                    Console.WriteLine("❌ Invalid generation session (Run ID: -1). Exiting watcher...");
                    cts.Cancel();
                    return;
                }
                
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
                                CliHelpers.ResetProgressArea();
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
                    CliHelpers.ResetProgressArea();
                    Console.WriteLine("✅ Generation completed. Exiting watcher...");
                    cts.Cancel();
                }
            }, cts.Token);
        }
        finally
        {
            verifyCts?.Cancel();
            verifyCts?.Dispose();
            CliHelpers.ResetProgressArea();
        }
    }

    private static void RenderPollingStatus(ActiveProjectResponse response)
    {
        Console.WriteLine($"Run ID: {response.ActiveRunId} | Project: {response.ProjectName} | Finished: {response.IsFinished}");
    }
}

