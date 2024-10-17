namespace Butternut.Tool;

using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Reflection;
using Spectre.Console;

internal static class Program
{
    private static readonly Lazy<FigletFont> LazyFigletFont = new(() =>
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(assembly.FindResourceFullName("Pepper.flf")!);
        return FigletFont.Load(stream!);
    });

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var parser = new CommandLineBuilder(new ButternutRootCommand())
                .UseDefaults()
                .UseHelp(context => context.HelpBuilder.CustomizeLayout(_ =>
                    HelpBuilder.Default.GetLayout().Prepend(
                        _ => AnsiConsole.Write(new FigletText(LazyFigletFont.Value, "butternut").LeftJustified()
                            .Color(Color.DarkOrange)))))
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
}