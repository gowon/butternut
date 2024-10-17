namespace Butternut.Tool;

using System.Text;
using Gherkin.Ast;

internal static class GherkinMarkdownExtensions
{
    public static string RenderTags(this IHasTags node)
    {
        if (!node.Tags.Any())
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"`{string.Join("`, `", node.Tags.Select(tag => tag.Name))}`");
        return builder.ToString();
    }

    public static string RenderSteps(this IHasSteps node)
    {
        var builder = new StringBuilder();
        foreach (var step in node.Steps)
        {
            builder.AppendLine($"- _{step.Keyword.Trim()}_ {step.Text.Replace("<", "`<").Replace(">", ">`")}");
            if (step.Argument is DataTable dataTable)
            {
                builder.Append(dataTable.AsMarkdown());
            }
        }

        return builder.ToString();
    }

    public static string AsMarkdown(this Step step)
    {
        var builder = new StringBuilder();
        builder.AppendLine($">_{step.Keyword.Trim()}_ {step.Text.Replace("<", "[").Replace(">", "]")}  ");

        if (step.Argument is DataTable dataTable)
        {
            builder.Append(dataTable.AsMarkdown());
        }

        return builder.ToString();
    }

    private static string AsMarkdown(this IHasRows dataTable)
    {
        var columnWith = ExtractColumnWith(dataTable);

        var builder = new StringBuilder();
        builder.AppendLine(">");
        var firstRow = dataTable.Rows.First();
        builder.AppendLine(AsTableRow(firstRow));
        builder.AppendLine("> " + firstRow.Cells.GenerateTableSeparator(columnWith));
        foreach (var row in dataTable.Rows.Skip(1)) builder.AppendLine(AsTableRow(row));
        builder.AppendLine(">");
        return builder.ToString();

        string AsTableRow(TableRow row)
        {
            return "> " + row.GenerateRow(columnWith);
        }
    }

    private static string GenerateTableSeparator(this IEnumerable<TableCell> tableCells,
        IReadOnlyList<int> columnWith)
    {
        return string.Join(string.Empty,
            tableCells.Select((cell, index) => $"| {new string('-', columnWith[index])} ")) + "|  ";
    }

    private static int[] ExtractColumnWith(this IHasRows table)
    {
        var columnWith = ColumnWith(table.Rows.First());
        foreach (var row in table.Rows.Skip(1))
        {
            var rowWiths = ColumnWith(row);

            for (var i = 0; i < columnWith.Length; i++)
                columnWith[i] = rowWiths[i] > columnWith[i] ? rowWiths[i] : columnWith[i];
        }

        return columnWith;

        static int[] ColumnWith(TableRow row)
        {
            return row.Cells.Select(cell => Math.Max(3, cell.Value.Length)).ToArray();
        }
    }

    public static string AsMarkdown(this IEnumerable<Examples> examples)
    {
        var builder = new StringBuilder();
        examples.Aggregate(builder, (_, example) => builder.Append(example.AsMarkdown()));
        return builder.ToString();
    }

    public static string AsMarkdown(this Examples examples)
    {
        var builder = new StringBuilder();
        builder.Append(examples.Keyword.AsMarkdownHeader(3, true));

        var columnWith = examples.ExtractColumnWith();

        builder.AppendLine(">" + examples.TableHeader.GenerateRow(columnWith));
        builder.AppendLine(">" + GenerateSeparationRow());
        foreach (var row in examples.TableBody) builder.AppendLine(">" + row.GenerateRow(columnWith));

        builder.AppendLine(">");
        return builder.ToString();

        string GenerateSeparationRow()
        {
            return string.Join(string.Empty,
                examples.TableHeader.Cells.Select((cell, index) => $"| {new string('-', columnWith[index])} ")) + "|";
        }
    }

    private static string GenerateRow(this TableRow row, IReadOnlyList<int> columnWith)
    {
        var cellArray = row.Cells.ToArray();

        var rowBuilder = new StringBuilder();
        for (var index = 0; index < cellArray.Length; index++)
            rowBuilder.Append($"| {cellArray[index].Value.PadRight(columnWith[index])} ");

        return rowBuilder + "|  ";
    }

    public static string AsMarkdown(this Scenario scenario)
    {
        var builder = new StringBuilder();

        //builder.Append(scenario.Name.AsMarkdownHeader(2));

        builder.AppendLine($"### Scenario: {scenario.Name}\n");

        
        builder.AppendLine(scenario.RenderTags());

        if (!string.IsNullOrWhiteSpace(scenario.Description))
        {
            builder.AppendLine(scenario.Description.Cleanup());
        }



        
        builder.AppendLine(scenario.RenderSteps());

        //scenario.Steps.Aggregate(builder, (_, step) => builder.Append(AsMarkdown(step)));

        if (scenario.Examples.Any())
        {
            builder.Append(scenario.Examples.AsMarkdown());
        }

        return builder.ToString();
    }

    private static string Cleanup(this string description)
    {
        return string.Join($"{Environment.NewLine}", description.Split(Environment.NewLine).Select(s => s.Trim()));
    }

    public static string AsMarkdown(this Background background)
    {
        var builder = new StringBuilder();
        background.Steps.Aggregate(builder, (s, step) => builder.Append(AsMarkdown(step)));
        return builder.ToString();
    }

    public static string AsMarkdown(this Rule rule)
    {
        var builder = new StringBuilder();

        builder.Append(rule.Name.AsMarkdownHeader(2));

        if (!string.IsNullOrWhiteSpace(rule.Description))
        {
            builder.AppendLine(rule.Description.Cleanup());
        }
        
        return builder.ToString();
    }

    public static string AsMarkdown(this GherkinDocument document)
    {
        return document.Feature.AsMarkdown();
    }




    public static string AsMarkdown(this Feature feature)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Feature: {feature.Name}\n");
        //builder.Append(feature.Name.AsMarkdownHeader(1));

        if (feature.Tags.Any())
        {
            builder.AppendLine($"`{string.Join("`, `", feature.Tags.Select(tag => tag.Name))}`\n");
        }

        if (!string.IsNullOrWhiteSpace(feature.Description))
        {
            builder.AppendLine(feature.Description.Cleanup()).AppendLine();
        }

        foreach (var child in feature.Children)
        {
            switch (child)
            {
                case Background background:
                    builder.Append(background.AsMarkdown());
                    break;
                case Scenario scenario:
                    builder.Append(scenario.AsMarkdown());
                    break;
                default:
                    continue;
                    throw new ArgumentOutOfRangeException(nameof(child),
                        $"the child type {child.GetType().Name} currently not supported");
            }
        }

        return builder.ToString();
    }

    private static string AsMarkdownHeader(this string value, int index, bool blockQuote = false)
    {
        var linePrefix = blockQuote ? ">" : string.Empty;
        var builder = new StringBuilder();
        builder.AppendLine(linePrefix);
        builder.AppendLine($"{linePrefix}{new string('#', index)} {value}");
        builder.AppendLine(linePrefix);
        return builder.ToString();
    }
}