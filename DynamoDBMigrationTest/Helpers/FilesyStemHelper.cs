namespace DynamoDBMigrationTest.Helpers;

internal static class FilesyStemHelper
{
    internal static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    internal static void CreateCsproj(string dir)
    {
        File.WriteAllText(
            Path.Combine(dir, "TestApp.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """
        );
    }

}
