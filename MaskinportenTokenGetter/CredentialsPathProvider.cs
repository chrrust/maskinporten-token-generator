namespace MaskinportenTokenGetter;

public static class CredentialsPathProvider
{
    public static string GetCredentialsFilePath(string fileName)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), 
            ApplicationData.Name);
        
        var path = string.IsNullOrWhiteSpace(basePath) ? fileName : Path.Combine(basePath, fileName);
        EnsureDirectoryExists(Path.GetDirectoryName(path)!);
        return path;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}