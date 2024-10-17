namespace build;

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Extensions;
using global::Extensions.Options.AutoBinder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Spectre.Console;

internal static class Program
{
    public const string DefaultConfigFile = "buildconfig.json";
    public static Option<FileInfo> ConfigurationFileGlobalOption = new("--config-file", "Specify configuration file");

    public static Option<DirectoryInfo>
        WorkingDirectoryGlobalOption = new("--working-dir", "Specify working directory");

    public static Option<LogLevel> VerbosityGlobalOption =
        new(new[] { "--log-verbosity" }, () => LogLevel.Warning, "Set log verbosity");

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var command = new RootCommand();
            command.RegisterCommandsInAssembly();
            command.AddGlobalOption(ConfigurationFileGlobalOption);
            command.AddGlobalOption(WorkingDirectoryGlobalOption);
            command.AddGlobalOption(VerbosityGlobalOption);

            command.SetHandler(context =>
            {
                // ref: https://github.com/dotnet/command-line-api/issues/1537
                context.HelpBuilder.CustomizeLayout(_ =>
                    HelpBuilder.Default.GetLayout().Skip(1).Prepend(
                        _ => AnsiConsole.Write(new FigletText("build tool").LeftJustified()
                            .Color(Color.DarkBlue))));
                context.HelpBuilder.Write(context.ParseResult.CommandResult.Command,
                    context.Console.Out.CreateTextWriter());
            });

            var parser = new CommandLineBuilder(command)
                .UseHost(CreateHostBuilder)
                .UseDefaults()
                .UseDebugDirective()
                .UseConfigurationDirective()
                .UseExceptionHandler((exception, context) =>
                {
                    Console.WriteLine($"Unhandled exception occurred: {exception.Message}");
                    context.ExitCode = 1;
                })
                .Build();

            return await parser.InvokeAsync(args);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Application terminated unexpectedly: {exception.Message}");
            return 1;
        }
    }

    // ref: https://github.com/dotnet/command-line-api/issues/2250
    // ref: https://github.com/dotnet/command-line-api/issues/1838#issuecomment-1242435714
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        HostBuilder builder = new();

        return builder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseServiceProviderFactory(
                new DefaultServiceProviderFactory(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                }))
            .ConfigureAppConfiguration((context, config) =>
            {
                var basePath = context.GetInvocationContext().GetWorkingDirectory();

                config
                    .SetBasePath(basePath)
                    .AddJsonFile(DefaultConfigFile, true, false)
                    .AddJsonFile($"{DefaultConfigFile}.user", true, false);

                var fileInfo = context.GetInvocationContext().ParseResult
                    .GetValueForOption(ConfigurationFileGlobalOption);

                if (fileInfo is { Exists: true })
                {
                    config.AddJsonFile(fileInfo.FullName, true, false);
                }

                config.AddEnvironmentVariables("BUILD_");
            })
            .ConfigureLogging((context, loggingBuilder) =>
            {
                var logLevel = context.GetInvocationContext().ParseResult
                    .GetValueForOption(VerbosityGlobalOption);

                loggingBuilder
                    .SetMinimumLevel(logLevel)
                    .AddDebug()
                    .AddConsole()
                    .AddFilter<DebugLoggerProvider>(level => level >= LogLevel.Debug);
            })
            .ConfigureServices((_, services) => { services.AutoBindOptions(); });
    }
}