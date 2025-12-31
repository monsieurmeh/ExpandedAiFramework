using MelonLoader.Utils;
using System;
using System.IO;
using System.Reflection;
public static class EmbeddedResourceExtractor
{
    public static void Extract(string fileName, string outfileFilePath)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Your namespace + folder path inside the DLL, adjust if needed
        string resourceName = FindResourceName(assembly, fileName);

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            MelonLogger.Error($"[ERROR] Embedded resource not found: {resourceName}");
            return;
        }

        using FileStream fileStream = new FileStream(outfileFilePath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fileStream);
        MelonLogger.Msg($"[INFO] Extracted {fileName} to {outfileFilePath}");
    }

    private static string FindResourceName(Assembly assembly, string fileName)
    {
        return assembly.GetManifestResourceNames()
                       .FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
    }
}
