﻿namespace build.Extensions;

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class SystemCommandLineExtensions
{
    public static string GetWorkingDirectory(this InvocationContext context)
    {
        var workingDirectory = context.ParseResult
            .GetValueForOption(Program.WorkingDirectoryGlobalOption);

        // question: should we ever default to the directory that the executable is running in? Environment.ProcessPath
        // https://www.hanselman.com/blog/how-do-i-find-which-directory-my-net-core-console-application-was-started-in-or-is-running-from
        // https://stackoverflow.com/a/97491/7644876
        return workingDirectory is { Exists: true } ? workingDirectory.FullName : Directory.GetCurrentDirectory();
    }

    public static void RegisterCommandsInAssembly(this RootCommand rootCommand, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false } &&
            type.IsSubclassOf(typeof(Command)) &&
            type.GetConstructors().Any(info => !info.GetParameters().Any()) &&
            type.GetCustomAttributes(typeof(NotMappedAttribute), false).Length == 0);

        foreach (var type in types)
        {
            rootCommand.AddCommand((Command)Activator.CreateInstance(type)!);
        }
    }

    // removed in https://github.com/dotnet/command-line-api/pull/1585
    // ref https://github.com/dotnet/command-line-api/blob/7e5307c186034691d46cf14ea18c09ea4e9e738d/src/System.CommandLine/Builder/CommandLineBuilderExtensions.cs#L228
    // ref https://github.com/dotnet/command-line-api/blob/7e5307c186034691d46cf14ea18c09ea4e9e738d/src/System.CommandLine/Properties/Resources.resx#L213
    public static CommandLineBuilder UseDebugDirective(this CommandLineBuilder builder)
    {
        builder.AddMiddleware(async (context, next) =>
        {
            if (context.ParseResult.Directives.Contains("debug"))
            {
                const string environmentVariableName = "DOTNET_COMMANDLINE_DEBUG_PROCESSES";
                var process = Process.GetCurrentProcess();
                var debuggableProcessNames = Environment.GetEnvironmentVariable(environmentVariableName);
                if (string.IsNullOrWhiteSpace(debuggableProcessNames))
                {
                    // DebugDirectiveExecutableNotSpecified
                    context.Console.Error.WriteLine(string.Format(
                        "Debug directive specified, but no process names are listed as allowed for debug.\r\nAdd your process name to the '{0}' environment variable.\r\nThe value of the variable should be the name of the processes, separated by a semi-colon ';', for example '{0}={1}",
                        environmentVariableName, process.ProcessName));
                    context.ExitCode = 1;
                    return;
                }

                var processNames = debuggableProcessNames.Split(';');
                if (processNames.Contains(process.ProcessName, StringComparer.Ordinal))
                {
                    var processId = process.Id;
                    // DebugDirectiveAttachToProcess
                    context.Console.Out.WriteLine(string.Format("Attach your debugger to process {0} ({1}).", processId,
                        process.ProcessName));
                    while (!Debugger.IsAttached)
                    {
                        await Task.Delay(500);
                    }
                }
                else
                {
                    // DebugDirectiveProcessNotIncludedInEnvironmentVariable
                    context.Console.Error.WriteLine(string.Format(
                        "Process name '{0}' is not included in the list of debuggable process names in the {1} environment variable ('{2}')",
                        process.ProcessName, environmentVariableName, debuggableProcessNames));
                    context.ExitCode = 1;
                    return;
                }
            }

            await next(context);
        }, (MiddlewareOrder)(-2300));

        return builder;
    }

    public static CommandLineBuilder UseConfigurationDirective(this CommandLineBuilder builder)
    {
        builder.AddMiddleware(async (context, next) =>
        {
            if (context.ParseResult.Directives.Contains("config"))
            {
                var configuration =
                    context.GetHost().Services.GetRequiredService<IConfiguration>() as IConfigurationRoot;
                context.Console.Out.WriteLine(configuration!.GetDebugView());
            }

            await next(context);
        });

        return builder;
    }
}