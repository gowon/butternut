namespace Butternut.Tool;

using System.Reflection;

internal static class AssemblyExtensions
{
    public static string? ReadResourceText(this Assembly assembly, string name)
    {
        var fullName = assembly.FindResourceFullName(name);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var stream = assembly.GetManifestResourceStream(fullName);
        if (stream == null)
        {
            return null;
        }

        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    public static byte[]? ReadResourceBytes(this Assembly assembly, string name)
    {
        var fullName = assembly.FindResourceFullName(name);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        using (var ms = new MemoryStream())
        {
            var stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null)
            {
                return null;
            }

            stream.CopyTo(ms);
            stream.Close();
            return ms.ToArray();
        }
    }

    public static string? FindResourceFullName(this Assembly assembly, string name)
    {
        // The resource path has the following form: [Namespace].[folder].[filename].[fileExstension]
        // This helper will pass the first match. This could lead to unexpected results if there
        // are multiple files of the same type with the same name.
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (resourceName.EndsWith(name))
            {
                return resourceName;
            }
        }

        return null;
    }
}