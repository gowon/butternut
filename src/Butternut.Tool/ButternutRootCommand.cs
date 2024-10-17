namespace Butternut.Tool;

using System.CommandLine;
using System.IO.Abstractions;
using System.Security.AccessControl;
using Gherkin;

public sealed class ButternutRootCommand : RootCommand
{
    public readonly Option<DirectoryInfo?> OutputDirectoryPathOption =
        new(new[] { "--output", "-o" }, "The folder that the artifacts will be stored in");

    public readonly Argument<DirectoryInfo?> SourceDirectoryPathArgument =
        new("source", "Source folder")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

    public readonly Option<bool> RecursiveOption = new(new[] { "--recursive", "-r" },
        "Search through the source folder recursively for all .feature files.");

    public readonly Option<bool> DryRunOption = new(new[] { "--dry-run", "-d" },
        "Output operation without performing the work.");

    public readonly Option<bool> JsonOption = new(new[] { "--json", "-j" },
        "Convert to JSON format");

    public ButternutRootCommand() : base("Something about squash")
    {
        AddArgument(SourceDirectoryPathArgument);
        AddOption(OutputDirectoryPathOption);
        AddOption(RecursiveOption);
        AddOption(DryRunOption);
        AddOption(JsonOption);

        this.SetHandler(async context =>
        {
            var cancellationToken = context.GetCancellationToken();
            var sourceDirectory = context.ParseResult.GetValueForArgument(SourceDirectoryPathArgument) ??
                                  new DirectoryInfo(Directory.GetCurrentDirectory());
            var outputDirectory = context.ParseResult.GetValueForOption(OutputDirectoryPathOption) ?? sourceDirectory;

            try
            {
                await ConvertGherkinFiles(new FileSystem(), new Parser(), sourceDirectory, outputDirectory,
                    cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private async Task ConvertGherkinFiles(IFileSystem fileSystem, Parser parser, DirectoryInfo sourceDirectory,
        DirectoryInfo outputDirectory, CancellationToken cancellationToken)
    {
        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }
        
        var files = sourceDirectory.GetFiles("*.feature", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var gherkinDocument = parser.Parse(file.FullName);
            var markdown = gherkinDocument.AsMarkdown();
            var outputPath = fileSystem.Path.Join(outputDirectory.FullName, $"{file.Name}.md");
            await fileSystem.File.WriteAllTextAsync(outputPath, markdown, cancellationToken);
        }
    }
}